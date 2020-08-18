using System;
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

namespace mrHelper.App.Interprocess
{
   internal class DiffCallHandler
   {
      internal DiffCallHandler(MatchInfo matchInfo, Snapshot snapshot,
         GitLabInstance gitLabInstance, IModificationListener modificationListener, User currentUser)
      {
         _matchInfo = matchInfo;
         _snapshot = snapshot;
         _gitLabInstance = gitLabInstance;
         _modificationListener = modificationListener;
         _currentUser = currentUser;
      }

      async public Task HandleAsync(ICommitStorage gitRepository)
      {
         if (_gitLabInstance == null || gitRepository == null)
         {
            Debug.Assert(false);
            return;
         }
         await doHandleAsync(gitRepository.Git);
      }

      async public Task doHandleAsync(IGitCommandService git)
      {
         FileNameMatcher fileNameMatcher = getFileNameMatcher(git);
         LineNumberMatcher lineNumberMatcher = new LineNumberMatcher(git);

         DiffPosition position = new DiffPosition(null, null, null, null, _snapshot.Refs);

         try
         {
            if (!fileNameMatcher.Match(_matchInfo, position, out position))
            {
               return;
            }

            lineNumberMatcher.Match(_matchInfo, position, out position);
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

         using (NewDiscussionForm form = new NewDiscussionForm(
            _matchInfo.LeftFileName, _matchInfo.RightFileName, position, git))
         {
            if (form.ShowDialog() == DialogResult.OK)
            {
               try
               {
                  await submitDiscussionAsync(position, form.Body, form.IncludeContext);
               }
               catch (DiscussionCreatorException ex)
               {
                  string message = "Cannot create a discussion at GitLab";
                  ExceptionHandlers.Handle(message, ex);
                  MessageBox.Show(String.Format("{0}. Check your connection and try again.", message),
                     "Error", MessageBoxButtons.OK, MessageBoxIcon.Error,
                     MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
               }
            }
         }
      }

      private FileNameMatcher getFileNameMatcher(IGitCommandService git)
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
            if (needSuppressWarning(currentName))
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
               addFileToWhitelist(currentName);
            }
            return isWarningIgnoredByUser;
         },
            (currentName) =>
         {
            if (needSuppressWarning(currentName))
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
               addFileToWhitelist(currentName);
            }
            return isWarningIgnoredByUser;
         });
      }

      async private Task submitDiscussionAsync(DiffPosition position, string body, bool includeContext)
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

         MergeRequestKey mrk = getMergeRequestKey(_snapshot);
         IDiscussionCreator creator = Shortcuts.GetDiscussionCreator(
            _gitLabInstance, _modificationListener, mrk, _currentUser);

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
                  _snapshot.Refs.ToString(),
                  _matchInfo.ToString(),
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

      private bool needSuppressWarning(string filename)
      {
         if (Program.Settings.SuppressWarningsOnFileMismatch)
         {
            return true;
         }

         MergeRequestKey mrk = getMergeRequestKey(_snapshot);
         MismatchWhitelistKey key = new MismatchWhitelistKey(mrk, filename);
         return _mismatchWhitelist.Contains(key);
      }

      private void addFileToWhitelist(string filename)
      {
         MergeRequestKey mrk = getMergeRequestKey(_snapshot);
         MismatchWhitelistKey key = new MismatchWhitelistKey(mrk, filename);
         _mismatchWhitelist.Add(key);
      }

      private static MergeRequestKey getMergeRequestKey(Snapshot snapshot)
      {
         ProjectKey projectKey = new ProjectKey(snapshot.Host, snapshot.Project);
         return new MergeRequestKey(projectKey, snapshot.MergeRequestIId);
      }

      private readonly MatchInfo _matchInfo;
      private readonly Snapshot _snapshot;
      private readonly GitLabInstance _gitLabInstance;
      private readonly IModificationListener _modificationListener;
      private readonly User _currentUser;

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

