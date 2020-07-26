using GitLabSharp.Entities;
using mrHelper.Client.Common;
using mrHelper.Client.Types;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.Discussions
{
   internal class DiscussionAccessor : IDiscussionAccessor
   {
      internal DiscussionAccessor(IHostProperties settings, MergeRequestKey mrk,
         ModificationNotifier modificationNotifier)
      {
         _settings = settings;
         _mrk = mrk;
         _modificationNotifier = modificationNotifier;
      }

      public IDiscussionCreator GetDiscussionCreator(User user)
      {
         DiscussionOperator discussionOperator = new DiscussionOperator(_mrk.ProjectKey.HostName, _settings);
         return new DiscussionCreator(_mrk, discussionOperator, user);
      }

      public ISingleDiscussionAccessor GetSingleDiscussionAccessor(string discussionId)
      {
         return new SingleDiscussionAccessor(_settings, _mrk, discussionId, _modificationNotifier);
      }

      private readonly IHostProperties _settings;
      private readonly MergeRequestKey _mrk;
      private readonly ModificationNotifier _modificationNotifier;
   }
}

