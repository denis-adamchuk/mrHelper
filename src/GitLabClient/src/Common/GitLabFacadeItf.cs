using mrHelper.Client.Discussions;
using mrHelper.Client.MergeRequests;
using mrHelper.Client.Repository;
using mrHelper.Client.TimeTracking;
using mrHelper.Client.Workflow;

namespace mrHelper.Client.Common
{
   public interface IGitLabFacade
   {
      IMergeRequestLoader MergeRequestLoader { get; }

      IMergeRequestManager MergeRequestManager { get; }

      IDiscussionManager DiscussionManager { get; }

      ITimeTrackingManager TimeTrackingManager { get; }
   }
}

