using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using mrHelper.Client.Types;
using mrHelper.Common.Interfaces;
using GitLabSharp;
using GitLabSharp.Entities;

namespace mrHelper.Client.Common
{
   internal class SearchManager : ISearchManager
   {
      internal SearchManager(IHostProperties settings)
      {
         _settings = settings;
      }

      async public Task<MergeRequest> SearchMergeRequestAsync(
         string hostname, string projectName, int mergeRequestIId)
      {
         GitLabTaskRunner client = new GitLabTaskRunner(hostname, _settings.GetAccessToken(hostname));
         try
         {
            SearchCriteria searchCriteria = new SearchCriteria(
               new object[] { new SearchByIId(projectName, mergeRequestIId) });
            IEnumerable<MergeRequest> mergeRequests =
               await CommonOperator.SearchMergeRequestsAsync(client, searchCriteria, null, true);
            return mergeRequests.Any() ? mergeRequests.First() : null;
         }
         catch (OperatorException)
         {
            return null;
         }
      }

      async public Task<User> GetCurrentUserAsync(string hostname)
      {
         GitLabTaskRunner client = new GitLabTaskRunner(hostname, _settings.GetAccessToken(hostname));
         try
         {
            return await CommonOperator.SearchCurrentUserAsync(client);
         }
         catch (OperatorException)
         {
            return null;
         }
      }

      async public Task<User> SearchUserByNameAsync(string hostname, string name, bool isUsername)
      {
         GitLabTaskRunner client = new GitLabTaskRunner(hostname, _settings.GetAccessToken(hostname));
         try
         {
            IEnumerable<User> users = await CommonOperator.SearchUserAsync(client, name, isUsername);
            return users.Any() ? users.First() : null;
         }
         catch (OperatorException)
         {
            return null;
         }
      }

      async public Task<Project> SearchProjectAsync(string hostname, string projectname)
      {
         GitLabTaskRunner client = new GitLabTaskRunner(hostname, _settings.GetAccessToken(hostname));
         try
         {
            return await CommonOperator.SearchProjectAsync(client, projectname);
         }
         catch (OperatorException)
         {
            return null;
         }
      }

      private readonly IHostProperties _settings;
   }
}

