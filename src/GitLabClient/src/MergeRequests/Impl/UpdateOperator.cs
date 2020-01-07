using System;
using System.Diagnostics;
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
         return CommonOperator.GetMergeRequestsAsync(client, project);
      }

      internal Task<Version> GetLatestVersionAsync(MergeRequestKey mrk)
      {
         GitLabClient client = new GitLabClient(mrk.ProjectKey.HostName,
            _settings.GetAccessToken(mrk.ProjectKey.HostName));
         return CommonOperator.GetLatestVersionAsync(client, mrk.ProjectKey.ProjectName, mrk.IId);
      }

      internal Task<MergeRequest> GetMergeRequestAsync(MergeRequestKey mrk)
      {
         GitLabClient client = new GitLabClient(mrk.ProjectKey.HostName,
            _settings.GetAccessToken(mrk.ProjectKey.HostName));
         return CommonOperator.GetMergeRequestAsync(client, mrk.ProjectKey.ProjectName, mrk.IId);
      }

      private readonly IHostProperties _settings;
   }
}

