﻿using System;
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
      internal RepositoryOperator(string host, string token)
      {
         _client = new GitLabClient(host, token);
      }

      async internal Task<Comparison> CompareAsync(string projectname, string from, string to)
      {
         try
         {
            return (Comparison)(await _client.RunAsync(async (gitlab) =>
               await gitlab.Projects.Get(projectname).Repository.CompareAsync(
                  new CompareParameters
                  {
                     From = from,
                     To = to
                  })));
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

      async internal Task<File> LoadFileAsync(string projectname, string filename, string sha)
      {
         try
         {
            return (File)(await _client.RunAsync(async (gitlab) =>
               await gitlab.Projects.Get(projectname).Repository.Files.
                  Get(filename).LoadTaskAsync(sha)));
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

      async internal Task CancelAsync()
      {
         await _client.CancelAsync();
      }

      private readonly GitLabClient _client;
   }
}

