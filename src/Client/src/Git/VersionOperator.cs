using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using GitLabSharp;
using GitLabSharp.Accessors;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;
using Version = GitLabSharp.Entities.Version;

namespace mrHelper.Client.Git
{
   /// <summary>
   /// Implements Version-oriented requests to GitLab
   /// </summary>
   internal class VersionOperator
   {
      internal VersionOperator(UserDefinedSettings settings)
      {
         _settings = settings;
      }

      async internal Task<List<Version>> LoadVersions(MergeRequestKey mrk)
      {
         GitLabClient client = new GitLabClient(mrk.ProjectKey.HostName,
            _settings.GetAccessToken(mrk.ProjectKey.HostName));
         try
         {
            return (List<Version>)(await client.RunAsync(async (gitlab) =>
               await gitlab.Projects.Get(mrk.ProjectKey.ProjectName).MergeRequests.Get(mrk.IId).
                  Versions.LoadAllTaskAsync()));
         }
         catch (Exception ex)
         {
            Debug.Assert(!(ex is GitLabClientCancelled));
            if (ex is GitLabSharpException || ex is GitLabRequestException)
            {
               ExceptionHandlers.Handle(ex, "Cannot load version list from GitLab");
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      async internal Task<Version> LoadVersion(Version version, MergeRequestKey mrk)
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
               ExceptionHandlers.Handle(ex, "Cannot load a version from GitLab");
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      private UserDefinedSettings _settings { get; }
   }
}


