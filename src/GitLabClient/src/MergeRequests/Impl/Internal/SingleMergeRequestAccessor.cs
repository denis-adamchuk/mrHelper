using mrHelper.Client.Common;
using mrHelper.Client.Discussions;
using mrHelper.Client.Types;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.MergeRequests
{
   internal class SingleMergeRequestAccessor : ISingleMergeRequestAccessor
   {
      internal SingleMergeRequestAccessor(IHostProperties settings, MergeRequestKey mrk,
         ModificationNotifier modificationNotifier)
      {
         _settings = settings;
         _mrk = mrk;
         _modificationNotifier = modificationNotifier;
      }

      public IMergeRequestEditor GetMergeRequestEditor()
      {
         MergeRequestOperator mergeRequestOperator = new MergeRequestOperator(_mrk.ProjectKey.HostName, _settings);
         return new MergeRequestEditor(mergeRequestOperator, _modificationNotifier);
      }

      public IDiscussionAccessor GetDiscussionAccessor()
      {
         return new DiscussionAccessor(_settings, _mrk, _modificationNotifier);
      }

      private readonly IHostProperties _settings;
      private readonly MergeRequestKey _mrk;
      private readonly ModificationNotifier _modificationNotifier;
   }
}

