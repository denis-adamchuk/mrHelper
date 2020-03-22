using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp;
using GitLabSharp.Entities;
using mrHelper.Client.Types;
using mrHelper.Client.Common;
using Version = GitLabSharp.Entities.Version;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.MergeRequests
{
   /// <summary>
   /// Implements Updates-related interaction with GitLab
   /// </summary>
   internal class UpdateOperator
   {
      internal UpdateOperator(IHostProperties settings)
      {
         _settings = settings;
      }

      internal Task<IEnumerable<MergeRequest>> GetMergeRequestsAsync(string host, string project)
      {
         GitLabClient client = new GitLabClient(host, _settings.GetAccessToken(host));
         SearchByProject searchByProject = new SearchByProject { ProjectName = project };
         return CommonOperator.SearchMergeRequestsAsync(client, searchByProject, null, true);
      }

      internal Task<Version> GetLatestVersionAsync(MergeRequestKey mrk)
      {
         GitLabClient client = new GitLabClient(mrk.ProjectKey.HostName,
            _settings.GetAccessToken(mrk.ProjectKey.HostName));
         return CommonOperator.GetLatestVersionAsync(client, mrk.ProjectKey.ProjectName, mrk.IId);
      }

      async internal Task<MergeRequest> GetMergeRequestAsync(MergeRequestKey mrk)
      {
         GitLabClient client = new GitLabClient(mrk.ProjectKey.HostName,
            _settings.GetAccessToken(mrk.ProjectKey.HostName));
         SearchByIId searchByIId = new SearchByIId { ProjectName = mrk.ProjectKey.ProjectName, IId = mrk.IId };
         IEnumerable<MergeRequest> mergeRequests =
            await CommonOperator.SearchMergeRequestsAsync(client, searchByIId, null, true);
         return mergeRequests.FirstOrDefault();
      }

      private readonly IHostProperties _settings;
   }
}

