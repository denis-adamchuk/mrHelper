using mrHelper.Client.Common;
using mrHelper.Client.Types;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.Discussions
{
   internal class SingleDiscussionAccessor : ISingleDiscussionAccessor
   {
      internal SingleDiscussionAccessor(IHostProperties settings, MergeRequestKey mrk, string discussionId,
         ModificationNotifier modificationNotifier)
      {
         _settings = settings;
         _mrk = mrk;
         _discussionId = discussionId;
         _modificationNotifier = modificationNotifier;
      }

      public IDiscussionEditor GetDiscussionEditor()
      {
         DiscussionOperator discussionOperator = new DiscussionOperator(_mrk.ProjectKey.HostName, _settings);
         return new DiscussionEditor(_mrk, _discussionId, discussionOperator,
            () =>
            {
               _modificationNotifier.OnDiscussionModified();
               //// TODO It can be removed when GitLab issue is fixed, see commit message
               //if (!_cachedDiscussions.ContainsKey(mrk))
               //{
               //   return;
               //}

               //Trace.TraceInformation(String.Format(
               //   "[DiscussionManager] Remove MR from cache after a Thread is (un)resolved: "
               // + "Host={0}, Project={1}, IId={2}",
               //   mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId.ToString()));
               //_cachedDiscussions.Remove(mrk);
            });
      }

      private readonly IHostProperties _settings;
      private readonly MergeRequestKey _mrk;
      private readonly string _discussionId;
      private readonly ModificationNotifier _modificationNotifier;
   }
}

