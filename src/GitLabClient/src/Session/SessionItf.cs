using System;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Client.Discussions;
using mrHelper.Client.MergeRequests;
using mrHelper.Client.TimeTracking;
using mrHelper.Client.Types;

namespace mrHelper.Client.Session
{
   public interface ISession : IDisposable
   {
      Task Start(string hostname, SessionContext context);

      IMergeRequestCache MergeRequestCache { get; }
      IDiscussionCache DiscussionCache { get; }
      ITotalTimeCache TotalTimeCache { get; }
      IProjectUpdateContextProviderFactory UpdateContextProviderFactory { get; }

      ITimeTracker GetTimeTracker(MergeRequestKey mrk);
      IDiscussionEditor GetDiscussionEditor(MergeRequestKey mrk, string discussionId);
      IDiscussionCreator GetDiscussionCreator(MergeRequestKey mrk);

      event Action Stopped;
      event Action<string> Starting;
      event Action<string, User> Started;
   }
}

