using System;
using System.Diagnostics;
using System.Threading.Tasks;
using GitLabSharp;
using GitLabSharp.Entities;
using GitLabSharp.Accessors;
using mrHelper.Client.Types;
using mrHelper.Client.Common;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.Repository
{
   /// <summary>
   /// Implements Repository-related interaction with GitLab
   /// </summary>
   internal class RepositoryOperator
   {
      internal RepositoryOperator(IHostProperties settings)
      {
         _settings = settings;
      }

      async internal Task<Comparison> CompareAsync(ProjectKey projectKey, string from, string to)
      {
         GitLabClient client = new GitLabClient(projectKey.HostName,
            _settings.GetAccessToken(projectKey.HostName));
         try
         {
            return (Comparison)(await client.RunAsync(async (gitlab) =>
               await gitlab.Projects.Get(projectKey.ProjectName).Repository.CompareAsync(
                  new CompareParameters
                  {
                     From = from,
                     To = to
                  })));
         }
         catch (Exception ex)
         {
            Debug.Assert(!(ex is GitLabClientCancelled));
            if (ex is GitLabSharpException || ex is GitLabRequestException)
            {
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      private readonly IHostProperties _settings;
   }
}

