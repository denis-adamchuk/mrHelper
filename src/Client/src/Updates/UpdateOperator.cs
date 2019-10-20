using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp;
using GitLabSharp.Accessors;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;
using mrHelper.Client.Common;
using Version = GitLabSharp.Entities.Version;

namespace mrHelper.Client.Updates
{
   /// <summary>
   /// Implements Updates-related interaction with GitLab
   /// </summary>
   internal class UpdateOperator
   {
      internal UpdateOperator(UserDefinedSettings settings)
      {
         Settings = settings;
      }

      internal Task<List<MergeRequest>> GetMergeRequestsAsync(string host, string project)
      {
         using (GitLabClient client = new GitLabClient(host, Settings.GetAccessToken(host)))
         {
            return CommonOperator.GetMergeRequestsAsync(client, project);
         }
      }

      internal Task<Version> GetLatestVersionAsync(MergeRequestKey mrk)
      {
         using (GitLabClient client = new GitLabClient(mrk.ProjectKey.HostName,
            Settings.GetAccessToken(mrk.ProjectKey.HostName)))
         {
            return CommonOperator.GetLatestVersionAsync(client, mrk.ProjectKey.ProjectName, mrk.IId);
         }
      }

      private UserDefinedSettings Settings { get; }
   }
}

