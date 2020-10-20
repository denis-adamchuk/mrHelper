using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp.Entities;
using GitLabSharp.Accessors;

namespace mrHelper.GitLabClient.Operators.Search
{
   internal class MergeRequestSearchProcessor
   {
      internal MergeRequestSearchProcessor(SearchQuery query)
      {
         _query = query;
      }

      async internal Task<IEnumerable<MergeRequest>> Process(GitLab gl)
      {
         // See restrictions at https://docs.gitlab.com/ee/api/README.html#offset-based-pagination
         Debug.Assert(!_query.MaxResults.HasValue || _query.MaxResults.Value <= 100);

         int? authorId = await getAuthorId(gl);
         int? iid = getIId();
         string labels = getLabels();
         string stateFilter = getStateFilter();
         MergeRequestsFilter.WorkInProgressFilter wipStatus = getWIPFilter();
         MergeRequestsFilter filter = new MergeRequestsFilter(labels, wipStatus, stateFilter, false, _query.Text,
            _query.TargetBranchName, iid.HasValue ? new int[] { iid.Value } : null, authorId);

         IEnumerable<MergeRequest> mergeRequests = await loadMergeRequests(gl, filter);
         return filterMergeRequests(iid, mergeRequests, stateFilter);
      }

      private int? getIId()
      {
         Debug.Assert(!_query.IId.HasValue || !String.IsNullOrWhiteSpace(_query.ProjectName));
         return _query.IId.HasValue ? _query.IId : null;
      }

      private MergeRequestsFilter.WorkInProgressFilter getWIPFilter()
      {
         return MergeRequestsFilter.WorkInProgressFilter.All;
      }

      private string getStateFilter()
      {
         return _query.State;
      }

      private string getLabels()
      {
         if (_query.Labels == null)
         {
            return null;
         }
         return String.Join(",", _query.Labels);
      }

      async private Task<int?> getAuthorId(GitLab gl)
      {
         if (!String.IsNullOrEmpty(_query.AuthorUserName))
         {
            User user = GlobalCache.GetUser(gl.Host, _query.AuthorUserName);
            if (user == null)
            {
               IEnumerable<User> users = await gl.Users.SearchByUsernameTaskAsync(_query.AuthorUserName);
               if (users == null || !users.Any())
               {
                  return null;
               }
               user = users.First();
               GlobalCache.AddUser(gl.Host, user);
            }
            return user.Id;
         }
         return null;
      }

      private Task<IEnumerable<MergeRequest>> loadMergeRequests(GitLab gl, MergeRequestsFilter filter)
      {
         if (_query.MaxResults.HasValue && !String.IsNullOrWhiteSpace(_query.ProjectName))
         {
            return gl
               .Projects
               .Get(_query.ProjectName)
               .MergeRequests
               .LoadTaskAsync(filter, getTrivialPageFilter());
         }
         else if (_query.MaxResults.HasValue)
         {
            return gl
               .MergeRequests
               .LoadTaskAsync(filter, getTrivialPageFilter());
         }
         else if (!String.IsNullOrWhiteSpace(_query.ProjectName))
         {
            return gl
               .Projects
               .Get(_query.ProjectName)
               .MergeRequests
               .LoadAllTaskAsync(filter);
         }
         else
         {
            return gl
               .MergeRequests
               .LoadAllTaskAsync(filter);
         }
      }

      private IEnumerable<MergeRequest> filterMergeRequests(int? iid, IEnumerable<MergeRequest> mergeRequests,
         string stateFilter)
      {
         if (iid.HasValue && mergeRequests.Any())
         {
            Debug.Assert(mergeRequests.Count() == 1);
            Debug.Assert(mergeRequests.First().IId == iid);

            MergeRequest mergeRequest = mergeRequests.First();
            if (stateFilter == null || stateFilter == mergeRequest.State)
            {
               return new MergeRequest[] { mergeRequest };
            }
            return Array.Empty<MergeRequest>();
         }
         return mergeRequests;
      }

      private PageFilter getTrivialPageFilter()
      {
         Debug.Assert(_query.MaxResults.HasValue);
         return new PageFilter(_query.MaxResults.Value, 1);
      }

      private SearchQuery _query;
   }
}

