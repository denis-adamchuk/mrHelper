using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitLabSharp.Accessors;
using mrHelper.App.Forms;
using mrHelper.Client.Discussions;
using mrHelper.Client.Git;
using mrHelper.Client.Tools;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;
using mrHelper.Core.Interprocess;
using mrHelper.Core.Matching;

namespace mrHelper.App
{
   internal class DiffCallHandler
   {
      internal DiffCallHandler(MatchInfo matchInfo, Snapshot snapshot)
      {
         _matchInfo = matchInfo;
         _snapshot = snapshot;
      }

      async public Task HandleAsync(IGitRepository gitRepository)
      {
         if (gitRepository != null)
         {
            await doHandleAsync(gitRepository);
            return;
         }

         Trace.TraceWarning(String.Format(
            "[DiffCallHandler] Creating temporary GitClient for TempFolder \"{0}\", Host {1}, Project {2}",
            _snapshot.TempFolder, _snapshot.Host, _snapshot.Project));

         using (GitClientFactory factory = new GitClientFactory(_snapshot.TempFolder, null, null))
         {
            GitClient tempRepository = factory.GetClient(_snapshot.Host, _snapshot.Project);
            Debug.Assert(!tempRepository.DoesRequireClone());
            await doHandleAsync(tempRepository);
         }
      }

      async public Task doHandleAsync(IGitRepository gitRepository)
      {
         FileNameMatcher fileNameMatcher = getFileNameMatcher(gitRepository);
         LineNumberMatcher lineNumberMatcher = new LineNumberMatcher(gitRepository);

         DiffPosition position = new DiffPosition
         {
            Refs = _snapshot.Refs
         };

         bool matchSucceded;
         try
         {
            matchSucceded = fileNameMatcher.Match(_matchInfo, position, out position) &&
                            lineNumberMatcher.Match(_matchInfo, position, out position);
         }
         catch (Exception ex)
         {
            Debug.Assert((ex is ArgumentException) || (ex is GitOperationException));
            ExceptionHandlers.Handle(ex, "Cannot create DiffPosition");
            return;
         }

         if (!matchSucceded)
         {
            return;
         }

         using (NewDiscussionForm form = new NewDiscussionForm(
            _matchInfo.LeftFileName, _matchInfo.RightFileName, position, gitRepository))
         {
            if (form.ShowDialog() == DialogResult.OK)
            {
               await submitDiscussionAsync(_snapshot, _matchInfo, position, form.Body, form.IncludeContext);
            }
         }
      }

      private FileNameMatcher getFileNameMatcher(IGitRepository repository)
      {
         return new FileNameMatcher(repository,
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

      async private static Task submitDiscussionAsync(Snapshot snapshot, MatchInfo matchInfo, DiffPosition position,
        string body, bool includeContext)
      {
         if (body.Length == 0)
         {
            MessageBox.Show("Discussion text cannot be empty", "Warning",
               MessageBoxButtons.OK, MessageBoxIcon.Exclamation,
               MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
            return;
         }

         NewDiscussionParameters parameters = new NewDiscussionParameters
         {
            Body = body,
            Position = includeContext ? createPositionParameters(position) : new Nullable<PositionParameters>()
         };

         MergeRequestDescriptor mergeRequestDescriptor = new MergeRequestDescriptor
         {
            HostName = snapshot.Host,
            ProjectName = snapshot.Project,
            IId = snapshot.MergeRequestIId
         };

         UserDefinedSettings settings = new UserDefinedSettings(false);
         DiscussionManager manager = new DiscussionManager(settings);
         DiscussionCreator creator = manager.GetDiscussionCreator(mergeRequestDescriptor);

         try
         {
            await creator.CreateDiscussionAsync(parameters);
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
         return new PositionParameters
         {
            OldPath = position.LeftPath,
            OldLine = position.LeftLine,
            NewPath = position.RightPath,
            NewLine = position.RightLine,
            BaseSHA = position.Refs.LeftSHA,
            HeadSHA = position.Refs.RightSHA,
            StartSHA = position.Refs.LeftSHA
         };
      }

      private readonly MatchInfo _matchInfo;
      private readonly Snapshot _snapshot;
   }
}

