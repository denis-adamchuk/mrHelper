using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
      internal DiffCallHandler(LineMatchInfo LineMatchInfo)
      {
         _originalLineMatchInfo = LineMatchInfo;
      }

      async public Task HandleAsync(Snapshot snapshot)
      {
         using (GitClientFactory factory = new GitClientFactory(snapshot.TempFolder, null))
         {
            IGitRepository gitRepository = factory.GetClient(snapshot.Host, snapshot.Project);
            LineMatchInfoCorrector corrector = getLineMatchInfoCorrector(gitRepository);

            LineMatchInfo? lineMatchInfo;
            try
            {
               lineMatchInfo = corrector.Correct(_originalLineMatchInfo, snapshot.Refs);
            }
            catch (GitOperationException ex)
            {
               ExceptionHandlers.Handle(ex, "Cannot create LineMatchInfo");
               return;
            }

            if (!lineMatchInfo.HasValue)
            {
               return;
            }

            RefToLineMatcher matcher = new RefToLineMatcher(gitRepository);
            DiffPosition position = matcher.Match(snapshot.Refs, lineMatchInfo.Value);

            NewDiscussionForm form = new NewDiscussionForm(
               lineMatchInfo.Value.LeftFileName, lineMatchInfo.Value.RightFileName, position, gitRepository);
            if (form.ShowDialog() == DialogResult.OK)
            {
               await submitDiscussionAsync(snapshot, lineMatchInfo.Value, position, form.Body, form.IncludeContext);
            }
         }
      }

      private LineMatchInfoCorrector getLineMatchInfoCorrector(IGitRepository repository)
      {
         return new LineMatchInfoCorrector(repository,
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
              MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            if (MessageBox.Show(
                     "Merge Request Helper detected that current file is a renamed version of another file. "
                     + question
                     + "It is recommended to press \"No\" and match files manually in the diff tool.\n"
                     + "Current file:\n"
                     + currentName + "\n\n"
                     + "Another file:\n"
                     + anotherName,
                     "Cannot create a discussion",
                     MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2)
               == DialogResult.No)
            {
               return false;
            }
            return true;
         },
            () =>
         {
            MessageBox.Show("Merge Request Helper detected that selected files do not match to each other. "
            + "GitLab does not allow to create discussions on such files.", "Cannot create a discussion",
              MessageBoxButtons.OK, MessageBoxIcon.Warning);
         });
      }

      async private static Task submitDiscussionAsync(Snapshot snapshot, LineMatchInfo lineMatchInfo, DiffPosition position,
        string body, bool includeContext)
      {
         if (body.Length == 0)
         {
            MessageBox.Show("Discussion text cannot be empty", "Warning",
               MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
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
            // TODO Check this place and other similar places if exceptions are caught (they seem to be not)
            await creator.CreateDiscussionAsync(parameters);
         }
         catch (DiscussionCreatorException ex)
         {
            Trace.TraceInformation(
                  "Additional information about exception:\n" +
                  "Position: {0}\n" +
                  "Include context: {1}\n" +
                  "Snapshot refs: {2}\n" +
                  "LineMatchInfo: {3}\n" +
                  "Body:\n{4}",
                  position.ToString(),
                  includeContext.ToString(),
                  snapshot.Refs.ToString(),
                  lineMatchInfo.ToString(),
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

      private readonly LineMatchInfo _originalLineMatchInfo;
   }
}

