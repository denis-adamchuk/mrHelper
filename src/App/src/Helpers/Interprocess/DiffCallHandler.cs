using System;
using System.Linq;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp.Entities;
using GitLabSharp.Accessors;
using mrHelper.App.Forms;
using mrHelper.App.Helpers.GitLab;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;
using mrHelper.Core.Matching;
using mrHelper.StorageSupport;
using mrHelper.GitLabClient;
using static mrHelper.App.Helpers.ConfigurationHelper;

namespace mrHelper.App.Interprocess
{
   internal class DiffCallHandler
   {
      internal DiffCallHandler(IGitCommandService git, IModificationListener modificationListener,
         User currentUser, Action<MergeRequestKey> onDiscussionSubmitted,
         Func<MergeRequestKey, IEnumerable<ReportedDiscussionNote>> getMyNotes,
         Action<MergeRequestKey> showDiscussions)
      {
         _git = git ?? throw new ArgumentException("git argument cannot be null");
         _modificationListener = modificationListener;
         _currentUser = currentUser;
         _onDiscussionSubmitted = onDiscussionSubmitted;
         _getMyNotes = getMyNotes;
         _showDiscussions = showDiscussions;
      }

      public void Handle(MatchInfo matchInfo, Snapshot snapshot)
      {
         FileNameMatcher fileNameMatcher = getFileNameMatcher(_git, getMergeRequestKey(snapshot));
         LineNumberMatcher lineNumberMatcher = new LineNumberMatcher(_git);

         DiffPosition position = new DiffPosition(null, null, null, null, snapshot.Refs);

         try
         {
            if (!fileNameMatcher.Match(matchInfo, position, out position))
            {
               return;
            }

            lineNumberMatcher.Match(matchInfo, position, out position);
         }
         catch (Exception ex)
         {
            if (ex is ArgumentException || ex is MatchingException)
            {
               ExceptionHandlers.Handle("Cannot create DiffPosition", ex);
               MessageBox.Show("Cannot create a discussion. Unexpected file name and/or line number passed",
                  "Error", MessageBoxButtons.OK, MessageBoxIcon.Error,
                  MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
               return;
            }
            throw;
         }

         MergeRequestKey mrk = getMergeRequestKey(snapshot);
         NewDiscussionForm form = new NewDiscussionForm(_git,
            position,
            async (body, includeContext) =>
            {
               try
               {
                  await submitDiscussionAsync(matchInfo, snapshot, position, body, includeContext);
               }
               catch (DiscussionCreatorException ex)
               {
                  string message = "Cannot create a discussion at GitLab";
                  ExceptionHandlers.Handle(message, ex);
                  MessageBox.Show(String.Format("{0}. Check your connection and try again.", message),
                     "Error", MessageBoxButtons.OK, MessageBoxIcon.Error,
                     MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
               }
            },
            _getMyNotes(mrk).ToArray(),
            async (notePosition, content) =>
            {
               try
               {
                  await editDiscussionNoteAsync(notePosition.DiscussionId, notePosition.Id, content.Body, snapshot);
               }
               catch (DiscussionEditorException ex)
               {
                  string message = "Cannot edit a discussion note at GitLab";
                  ExceptionHandlers.Handle(message, ex);
                  MessageBox.Show(String.Format("{0}. Check your connection and try again.", message),
                     "Error", MessageBoxButtons.OK, MessageBoxIcon.Error,
                     MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
               }
            },
            () => _showDiscussions?.Invoke(mrk),
            () => _onDiscussionSubmitted?.Invoke(mrk));
         form.Show();
      }

      private Task editDiscussionNoteAsync(string discussionId, int noteId, string text, Snapshot snapshot)
      {
         MergeRequestKey mrk = getMergeRequestKey(snapshot);
         GitLabInstance gitLabInstance = new GitLabInstance(snapshot.Host, Program.Settings);
         IDiscussionEditor editor = Shortcuts.GetDiscussionEditor(
            gitLabInstance, _modificationListener, mrk, discussionId);
         return editor.ModifyNoteBodyAsync(noteId, text);
      }

      private FileNameMatcher getFileNameMatcher(IGitCommandService git, MergeRequestKey mrk)
      {
         return new FileNameMatcher(git,
            (currentName, anotherName) =>
         {
            MessageBox.Show(
               "Merge Request Helper detected that current file is a moved version of another file. "
               + "GitLab does not allow to create discussions on moved files.\n\n"
               + "Current file:\n"
               + currentName + "\n\n"
               + "Another file:\n"
               + anotherName,
               "Cannot create a discussion",
               MessageBoxButtons.OK, MessageBoxIcon.Warning,
               MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
         },
            (currentName, anotherName, status) =>
         {
            if (needSuppressWarning(currentName, mrk))
            {
               return true;
            }

            string question = String.Empty;
            if (status == "new" || status == "deleted")
            {
               question = "Do you really want to review this file as a " + status + " file? ";
            }
            else if (status == "modified")
            {
               question = "Do you really want to continue reviewing this file against the selected file? ";
            }
            else
            {
               Debug.Assert(false);
            }

            bool isWarningIgnoredByUser = MessageBox.Show(
                  "Merge Request Helper detected that current file is a renamed version of another file. "
                  + question
                  + "It is recommended to press \"No\" and match files manually in the diff tool.\n"
                  + "Current file:\n"
                  + currentName + "\n\n"
                  + "Another file:\n"
                  + anotherName,
                  "Rename detected",
                  MessageBoxButtons.YesNo, MessageBoxIcon.Information,
                  MessageBoxDefaultButton.Button2, MessageBoxOptions.ServiceNotification)
               == DialogResult.Yes;
            if (isWarningIgnoredByUser)
            {
               addFileToWhitelist(currentName, mrk);
            }
            return isWarningIgnoredByUser;
         },
            (currentName) =>
         {
            if (needSuppressWarning(currentName, mrk))
            {
               return true;
            }

            string question = "Do you really want to continue reviewing this file against the selected file? ";
            bool isWarningIgnoredByUser = MessageBox.Show(
                  "Merge Request Helper detected that selected files do not match to each other. "
                  + question, "Files do not match",
                  MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                  MessageBoxDefaultButton.Button2, MessageBoxOptions.ServiceNotification)
               == DialogResult.Yes;
            if (isWarningIgnoredByUser)
            {
               addFileToWhitelist(currentName, mrk);
            }
            return isWarningIgnoredByUser;
         });
      }

      async private Task submitDiscussionAsync(MatchInfo matchInfo, Snapshot snapshot, DiffPosition position,
         string body, bool includeContext)
      {
         if (body.Length == 0)
         {
            MessageBox.Show("Discussion text cannot be empty", "Warning",
               MessageBoxButtons.OK, MessageBoxIcon.Exclamation,
               MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
            return;
         }

         NewDiscussionParameters parameters = new NewDiscussionParameters(
            body, includeContext ? createPositionParameters(position) : new PositionParameters?());

         MergeRequestKey mrk = getMergeRequestKey(snapshot);
         GitLabInstance gitLabInstance = new GitLabInstance(snapshot.Host, Program.Settings);
         IDiscussionCreator creator = Shortcuts.GetDiscussionCreator(
            gitLabInstance, _modificationListener, mrk, _currentUser);

         try
         {
            await creator.CreateDiscussionAsync(parameters, true);
         }
         catch (DiscussionCreatorException ex)
         {
            Trace.TraceInformation(
                  "Additional information about exception:\n" +
                  "Position: {0}\n" +
                  "Include context: {1}\n" +
                  "Snapshot refs: {2}\n" +
                  "MatchInfo: {3}\n" +
                  "Body:\n{4}",
                  position.ToString(),
                  includeContext.ToString(),
                  snapshot.Refs.ToString(),
                  matchInfo.ToString(),
                  body);

            if (!ex.Handled)
            {
               throw;
            }
         }
      }

      private static PositionParameters createPositionParameters(DiffPosition position)
      {
         return new PositionParameters(position.LeftPath, position.RightPath, position.LeftLine,
            position.RightLine, position.Refs.LeftSHA, position.Refs.RightSHA, position.Refs.LeftSHA);
      }

      private bool needSuppressWarning(string filename, MergeRequestKey mrk)
      {
         switch (GetShowWarningsOnFileMismatchMode(Program.Settings))
         {
            case ShowWarningsOnFileMismatchMode.Always:
               return false;

            case ShowWarningsOnFileMismatchMode.Never:
               return true;

            case ShowWarningsOnFileMismatchMode.UntilUserIgnoresFile:
               {
                  MismatchWhitelistKey key = new MismatchWhitelistKey(mrk, filename);
                  return _mismatchWhitelist.Contains(key);
               }
         }

         Debug.Assert(false);
         return false;
      }

      private void addFileToWhitelist(string filename, MergeRequestKey mrk)
      {
         MismatchWhitelistKey key = new MismatchWhitelistKey(mrk, filename);
         _mismatchWhitelist.Add(key);
      }

      private static MergeRequestKey getMergeRequestKey(Snapshot snapshot)
      {
         ProjectKey projectKey = new ProjectKey(snapshot.Host, snapshot.Project);
         return new MergeRequestKey(projectKey, snapshot.MergeRequestIId);
      }

      private readonly IGitCommandService _git;
      private readonly IModificationListener _modificationListener;
      private readonly User _currentUser;
      private readonly Action<MergeRequestKey> _onDiscussionSubmitted;
      private readonly Func<MergeRequestKey, IEnumerable<ReportedDiscussionNote>> _getMyNotes;
      private readonly Action<MergeRequestKey> _showDiscussions;

      private struct MismatchWhitelistKey : IEquatable<MismatchWhitelistKey>
      {
         public MismatchWhitelistKey(MergeRequestKey mergeRequestKey, string fileName) : this()
         {
            MergeRequestKey = mergeRequestKey;
            FileName = fileName;
         }

         internal MergeRequestKey MergeRequestKey { get; }
         internal string FileName { get; }

         public override bool Equals(object obj)
         {
            return obj is MismatchWhitelistKey && Equals((MismatchWhitelistKey)obj);
         }

         public bool Equals(MismatchWhitelistKey other)
         {
            return MergeRequestKey.Equals(other.MergeRequestKey) &&
                   FileName == other.FileName;
         }

         public override int GetHashCode()
         {
            var hashCode = 1704511527;
            hashCode = hashCode * -1521134295 + EqualityComparer<MergeRequestKey>.Default.GetHashCode(MergeRequestKey);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(FileName);
            return hashCode;
         }
      }

      private static readonly HashSet<MismatchWhitelistKey> _mismatchWhitelist = new HashSet<MismatchWhitelistKey>();
   }
}

