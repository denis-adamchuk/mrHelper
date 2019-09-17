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
         GitLabClient client = new GitLabClient(host, Tools.Tools.GetAccessToken(host, Settings));
         return CommonOperator.GetMergeRequestsAsync(client, project);
      }

      internal Task<Version> GetLatestVersionAsync(MergeRequestDescriptor mrd)
      {
         GitLabClient client = new GitLabClient(mrd.HostName, Tools.Tools.GetAccessToken(mrd.HostName, Settings));
         return CommonOperator.GetLatestVersionAsync(client, mrd.ProjectName, mrd.IId);
      }

      private UserDefinedSettings Settings { get; }
   }
}

