using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Client.Common;
using mrHelper.Client.Types;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.MergeRequests
{
   internal class MergeRequestAccessor : IMergeRequestAccessor
   {
      internal MergeRequestAccessor(IHostProperties settings, ProjectKey projectKey,
         ModificationNotifier modificationNotifier)
      {
         _settings = settings;
         _projectKey = projectKey;
         _modificationNotifier = modificationNotifier;
      }

      async public Task<MergeRequest> SearchMergeRequestAsync(int mergeRequestIId)
      {
         MergeRequestOperator mergeRequestOperator = new MergeRequestOperator(_projectKey.HostName, _settings);
         try
         {
            SearchCriteria searchCriteria = new SearchCriteria(
               new object[] { new SearchByIId(_projectKey.ProjectName, mergeRequestIId) });
            IEnumerable<MergeRequest> mergeRequests =
               await mergeRequestOperator.SearchMergeRequestsAsync(searchCriteria, null, true);
            return mergeRequests.Any() ? mergeRequests.First() : null;
         }
         catch (OperatorException)
         {
            return null;
         }
      }

      public IMergeRequestCreator GetMergeRequestCreator()
      {
         MergeRequestOperator mergeRequestOperator = new MergeRequestOperator(_projectKey.HostName, _settings);
         return new MergeRequestCreator(mergeRequestOperator);
      }

      public ISingleMergeRequestAccessor GetSingleMergeRequestAccessor(int iid)
      {
         return new SingleMergeRequestAccessor(_settings, new MergeRequestKey(_projectKey, iid), _modificationNotifier);
      }

      private readonly IHostProperties _settings;
      private readonly ProjectKey _projectKey;
      private readonly ModificationNotifier _modificationNotifier;
   }
}

