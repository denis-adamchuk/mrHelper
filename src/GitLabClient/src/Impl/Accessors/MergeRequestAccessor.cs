using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient.Operators;
using mrHelper.Common.Exceptions;
using mrHelper.GitLabClient.Accessors;

namespace mrHelper.GitLabClient
{
   public class MergeRequestAccessorException : ExceptionEx
   {
      internal MergeRequestAccessorException(string message, Exception innerException)
         : base(message, innerException)
      {
      }
   }

   public class MergeRequestAccessor
   {
      internal MergeRequestAccessor(IHostProperties settings, ProjectKey projectKey,
         IModificationListener modificationListener)
      {
         _settings = settings;
         _projectKey = projectKey;
         _modificationListener = modificationListener;
      }

      async public Task<MergeRequest> SearchMergeRequestAsync(int mergeRequestIId, bool onlyOpen)
      {
         using (MergeRequestOperator mergeRequestOperator = new MergeRequestOperator(
            _projectKey.HostName, _settings))
         {
            try
            {
               SearchCriteria searchCriteria = new SearchCriteria(
                  new object[] { new SearchByIId(_projectKey.ProjectName, mergeRequestIId) }, onlyOpen);
               IEnumerable<MergeRequest> mergeRequests =
                  await mergeRequestOperator.SearchMergeRequestsAsync(searchCriteria);
               return mergeRequests.Any() ? mergeRequests.First() : null;
            }
            catch (OperatorException)
            {
               return null;
            }
         }
      }

      public MergeRequestCreator GetMergeRequestCreator()
      {
         return new MergeRequestCreator(_projectKey, _settings);
      }

      public SingleMergeRequestAccessor GetSingleMergeRequestAccessor(int iid)
      {
         return new SingleMergeRequestAccessor(_settings, new MergeRequestKey(_projectKey, iid), _modificationListener);
      }

      private readonly IHostProperties _settings;
      private readonly ProjectKey _projectKey;
      private readonly IModificationListener _modificationListener;
   }
}

