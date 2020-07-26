using mrHelper.Client.Types;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.MergeRequests
{
   internal class MergeRequestAccessor : IMergeRequestAccessor
   {
      internal MergeRequestAccessor(IHostProperties settings, ProjectKey projectKey)
      {
         _settings = settings;
         _projectKey = projectKey;
      }

      public IMergeRequestCreator GetMergeRequestCreator()
      {
         MergeRequestOperator mergeRequestOperator = new MergeRequestOperator(_projectKey.HostName, _settings);
         return new MergeRequestCreator(mergeRequestOperator);
      }

      public ISingleMergeRequestAccessor GetSingleMergeRequestAccessor(int iid)
      {
         return new SingleMergeRequestAccessor(_settings, new MergeRequestKey(_projectKey, iid));
      }

      private readonly IHostProperties _settings;
      private readonly ProjectKey _projectKey;
   }
}

