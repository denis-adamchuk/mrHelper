using System.Collections.Generic;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Client.Discussions;
using mrHelper.Client.Types;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.MergeRequests
{
   internal class SingleMergeRequestAccessor : ISingleMergeRequestAccessor
   {
      internal SingleMergeRequestAccessor(IHostProperties settings, MergeRequestKey mrk)
      {
         _settings = settings;
         _mrk = mrk;
      }

      public IDiscussionCreator GetDiscussionCreator(User user)
      {
         DiscussionOperator discussionOperator = new DiscussionOperator(_mrk.ProjectKey.HostName, _settings);
         return new DiscussionCreator(_mrk, discussionOperator, user);
      }

      private readonly IHostProperties _settings;
      private readonly MergeRequestKey _mrk;
   }
}

