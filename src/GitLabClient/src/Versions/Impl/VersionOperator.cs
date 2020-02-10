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
      internal VersionOperator(IHostProperties settings)
      {
         _settings = settings;
      }

      async internal Task<IEnumerable<Version>> LoadVersionsAsync(MergeRequestKey mrk)
      {
         GitLabClient client = new GitLabClient(mrk.ProjectKey.HostName,
            _settings.GetAccessToken(mrk.ProjectKey.HostName));
         try
         {
            return (IEnumerable<Version>)(await client.RunAsync(async (gitlab) =>
               await gitlab.Projects.Get(mrk.ProjectKey.ProjectName).MergeRequests.Get(mrk.IId).
                  Versions.LoadAllTaskAsync()));
         }
         catch (Exception ex)
         {
            Debug.Assert(!(ex is GitLabClientCancelled));
            if (ex is GitLabSharpException || ex is GitLabRequestException)
            {
               GitLabExceptionHandlers.Handle(ex, "Cannot load version list from GitLab");
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      async internal Task<Version> LoadVersionAsync(Version version, MergeRequestKey mrk)
      {
         GitLabClient client = new GitLabClient(mrk.ProjectKey.HostName,
            _settings.GetAccessToken(mrk.ProjectKey.HostName));
         try
         {
            return (Version)(await client.RunAsync(async (gitlab) =>
               await gitlab.Projects.Get(mrk.ProjectKey.ProjectName).MergeRequests.Get(mrk.IId).
                  Versions.Get(version.Id).LoadTaskAsync()));
         }
         catch (Exception ex)
         {
            Debug.Assert(!(ex is GitLabClientCancelled));
            if (ex is GitLabSharpException || ex is GitLabRequestException)
            {
               GitLabExceptionHandlers.Handle(ex, "Cannot load a version from GitLab");
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      internal Task<Version> GetLatestVersionAsync(MergeRequestKey mrk)
      {
         GitLabClient client = new GitLabClient(mrk.ProjectKey.HostName,
            _settings.GetAccessToken(mrk.ProjectKey.HostName));
         return CommonOperator.GetLatestVersionAsync(client, mrk.ProjectKey.ProjectName, mrk.IId);
      }

      private readonly IHostProperties _settings;
   }
}

