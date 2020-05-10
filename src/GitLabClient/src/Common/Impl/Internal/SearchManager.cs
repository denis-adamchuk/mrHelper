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

      async public Task<MergeRequest?> SearchMergeRequestAsync(
         string hostname, string projectName, int mergeRequestIId)
      {
         GitLabClient client = new GitLabClient(hostname, _settings.GetAccessToken(hostname));
         try
         {
            SearchByIId searchByIId = new SearchByIId { ProjectName = projectName, IId = mergeRequestIId };
            IEnumerable<MergeRequest> mergeRequests =
               await CommonOperator.SearchMergeRequestsAsync(client, searchByIId, null, true);
            return mergeRequests.Any() ? mergeRequests.First() : new Nullable<MergeRequest>();
         }
         catch (OperatorException)
         {
            return null;
         }
      }

      async public Task<User?> SearchUserAsync(string hostname, string name)
      {
         GitLabClient client = new GitLabClient(hostname, _settings.GetAccessToken(hostname));
         try
         {
            IEnumerable<User> users = await CommonOperator.SearchUserAsync(client, name);
            return users.Any() ? users.First() : new Nullable<User>();
         }
         catch (OperatorException)
         {
            return null;
         }
      }

      async public Task<Project?> SearchProjectAsync(string hostname, string projectname)
      {
         GitLabClient client = new GitLabClient(hostname, _settings.GetAccessToken(hostname));
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

