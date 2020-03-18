using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using GitLabSharp;
using GitLabSharp.Accessors;
using mrHelper.Client.Common;
using mrHelper.Client.Types;
using mrHelper.Common.Interfaces;
using Version = GitLabSharp.Entities.Version;

namespace mrHelper.Client.Versions
{
   /// <summary>
   /// Implements Version-oriented requests to GitLab
   /// </summary>
   internal class VersionOperator
   {
      internal VersionOperator(string host, string token)
      {
         _client = new GitLabClient(host, token);
      }

      async internal Task<IEnumerable<Version>> LoadVersionsAsync(MergeRequestKey mrk)
      {
         try
         {
            return (IEnumerable<Version>)(await _client.RunAsync(async (gitlab) =>
               await gitlab.Projects.Get(mrk.ProjectKey.ProjectName).MergeRequests.Get(mrk.IId).
                  Versions.LoadAllTaskAsync()));
         }
         catch (Exception ex)
         {
            if (ex is GitLabSharpException || ex is GitLabRequestException || ex is GitLabClientCancelled)
            {
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      async internal Task<Version> LoadVersionAsync(Version version, MergeRequestKey mrk)
      {
         try
         {
            return (Version)(await _client.RunAsync(async (gitlab) =>
               await gitlab.Projects.Get(mrk.ProjectKey.ProjectName).MergeRequests.Get(mrk.IId).
                  Versions.Get(version.Id).LoadTaskAsync()));
         }
         catch (Exception ex)
         {
            if (ex is GitLabSharpException || ex is GitLabRequestException || ex is GitLabClientCancelled)
            {
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      internal Task<Version> GetLatestVersionAsync(MergeRequestKey mrk)
      {
         return CommonOperator.GetLatestVersionAsync(_client, mrk.ProjectKey.ProjectName, mrk.IId);
      }

      async internal Task CancelAsync()
      {
         await _client.CancelAsync();
      }

      private readonly GitLabClient _client;
   }
}

