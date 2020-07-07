using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitLabSharp.Accessors;
using mrHelper.App.Forms;
using mrHelper.App.Helpers;
using mrHelper.Client.Types;
using mrHelper.Client.Discussions;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;
using mrHelper.Core.Matching;
using mrHelper.StorageSupport;
using mrHelper.Client.Session;

namespace mrHelper.App.Interprocess
{
   internal class DiffCallHandler
   {
      internal DiffCallHandler(MatchInfo matchInfo, Snapshot snapshot, ISession session)
      {
         _matchInfo = matchInfo;
         _snapshot = snapshot;
         _session = session;
      }

      async public Task HandleAsync(ICommitStorage gitRepository)
      {
         if (gitRepository != null)
         {
            await doHandleAsync(gitRepository.Git);
            return;
         }

         // This happens when a git parent folder was changed when a diff tool was already launched
         Trace.TraceWarning(String.Format(
            "[DiffCallHandler] Creating temporary GitRepo for TempFolder \"{0}\", Host {1}, Project {2}",
            _snapshot.TempFolder, _snapshot.Host, _snapshot.Project));

         ProjectKey projectKey = new ProjectKey(_snapshot.Host, _snapshot.Project);

         LocalCommitStorageFactory factory = new LocalCommitStorageFactory(
            _snapshot.TempFolder, null, Program.Settings.UseShallowClone, _session, Program.Settings.RevisionsToKeep);
         LocalCommitStorageType type = ConfigurationHelper.GetPreferredStorageType(Program.Settings);
         ILocalCommitStorage tempRepository = factory.GetStorage(projectKey, type);
         if (tempRepository == null)
         {
            Trace.TraceError("[DiffCallHandler] Cannot create a temporary GitRepo");
            return;
         }
         await doHandleAsync(tempRepository.Git);
         factory.Dispose();
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
                  await submitDiscussionAsync(_snapshot, _matchInfo, position, form.Body, form.IncludeContext);
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
            return MessageBox.Show(
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
         },
            () =>
         {
            string question = "Do you really want to continue reviewing this file against the selected file? ";
            return MessageBox.Show(
                  "Merge Request Helper detected that selected files do not match to each other. "
                  + question, "Files do not match",
                  MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                  MessageBoxDefaultButton.Button2, MessageBoxOptions.ServiceNotification)
               == DialogResult.Yes;
         });
      }

      async private Task submitDiscussionAsync(Snapshot snapshot, MatchInfo matchInfo, DiffPosition position,
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

         MergeRequestKey mergeRequestKey = new MergeRequestKey(
            new ProjectKey(snapshot.Host, snapshot.Project), snapshot.MergeRequestIId);
         IDiscussionCreator creator = _session.GetDiscussionCreator(mergeRequestKey);

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

      private readonly MatchInfo _matchInfo;
      private readonly Snapshot _snapshot;
      private readonly ISession _session;
   }
}

