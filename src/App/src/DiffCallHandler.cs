using GitLabSharp.Accessors;
using mrHelper.App.Forms;
using mrHelper.Client.Discussions;
using mrHelper.Client.Git;
using mrHelper.Client.Tools;
using mrHelper.Common.Interfaces;
using mrHelper.Core.Interprocess;
using mrHelper.Core.Matching;
using mrHelper.Forms.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace mrHelper.App
{
   internal interface IInterprocessCallHandler
   {
      void Handle(Snapshot snapshot);
   }

   internal class DiffCallHandler : IInterprocessCallHandler
   {
      internal DiffCallHandler(DiffToolInfo diffToolInfo)
      {
         _originalDiffToolInfo = diffToolInfo;
      }

      public void Handle(Snapshot snapshot)
      {
         using (GitClientFactory factory = new GitClientFactory(snapshot.TempFolder, null))
         {
            IGitRepository gitRepository = factory.GetClient(snapshot.Host, snapshot.Project);

            DiffToolInfoProcessor processor = new DiffToolInfoProcessor(gitRepository);

            DiffToolInfo diffToolInfo;
            if (!processor.Process(_originalDiffToolInfo, snapshot.Refs, out diffToolInfo))
            {
               return;
            }

            RefToLineMatcher matcher = new RefToLineMatcher(gitRepository);
            DiffPosition position = matcher.Match(snapshot.Refs, diffToolInfo);

            NewDiscussionForm form = new NewDiscussionForm(snapshot, diffToolInfo, position, gitRepository);
            if (form.ShowDialog() == DialogResult.OK)
            {
               submitDiscussion(snapshot, diffToolInfo, position, form.Body, form.IncludeContext);
            }
         }
      }

      private static void submitDiscussion(Snapshot snapshot, DiffToolInfo diffToolInfo, DiffPosition position,
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
            Task.Run(async () => await creator.CreateDiscussionAsync(parameters));
         }
         catch (DiscussionCreatorException ex)
         {
            Trace.TraceInformation(
                  "Additional information about exception:\n" +
                  "Position: {0}\n" +
                  "Include context: {1}\n" +
                  "Snapshot refs: {2}\n" +
                  "DiffToolInfo: {3}\n" +
                  "Body:\n{4}",
                  position.ToString(),
                  includeContext.ToString(),
                  snapshot.Refs.ToString(),
                  diffToolInfo.ToString(),
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

      private readonly DiffToolInfo _originalDiffToolInfo;
   }
}

