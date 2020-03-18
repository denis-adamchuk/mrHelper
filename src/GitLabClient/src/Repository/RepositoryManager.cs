﻿using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Client.Types;
using mrHelper.Common.Interfaces;
using mrHelper.Client.Common;
using mrHelper.Common.Exceptions;

namespace mrHelper.Client.Repository
{
   public class RepositoryManagerException : ExceptionEx
   {
      internal RepositoryManagerException(string message, Exception innerException)
         : base(message, innerException)
      {
      }
   }

   public class RepositoryManager
   {
      public RepositoryManager(IHostProperties settings)
      {
         _settings = settings;
      }

      async public Task<Comparison?> CompareAsync(ProjectKey projectKey, string from, string to)
      {
         _operator = new RepositoryOperator(projectKey.HostName,
            _settings.GetAccessToken(projectKey.HostName));
         try
         {
            return await _operator.CompareAsync(projectKey.ProjectName, from, to);
         }
         catch (OperatorException ex)
         {
            if (ex.InnerException is GitLabSharp.GitLabClientCancelled)
            {
               return null;
            }
            throw new RepositoryManagerException("Cannot perform comparison", ex);
         }
      }

      async public Task<File?> LoadFileAsync(ProjectKey projectKey, string filename, string sha)
      {
         _operator = new RepositoryOperator(projectKey.HostName,
            _settings.GetAccessToken(projectKey.HostName));
         try
         {
            return await _operator.LoadFileAsync(projectKey.ProjectName, filename, sha);
         }
         catch (OperatorException ex)
         {
            if (ex.InnerException is GitLabSharp.GitLabClientCancelled)
            {
               return null;
            }
            throw new RepositoryManagerException("Cannot load file", ex);
         }
      }

      async public Task<Commit?> LoadCommitAsync(ProjectKey projectKey, string sha)
      {
         _operator = new RepositoryOperator(projectKey.HostName,
            _settings.GetAccessToken(projectKey.HostName));
         try
         {
            return await _operator.LoadCommitAsync(projectKey.ProjectName, sha);
         }
         catch (OperatorException ex)
         {
            if (ex.InnerException is GitLabSharp.GitLabClientCancelled)
            {
               return null;
            }
            throw new RepositoryManagerException("Cannot load commit", ex);
         }
      }

      async public Task<Branch?> CreateNewBranchAsync(ProjectKey projectKey, string name, string sha)
      {
         _operator = new RepositoryOperator(projectKey.HostName,
            _settings.GetAccessToken(projectKey.HostName));
         try
         {
            return await _operator.CreateNewBranchAsync(projectKey.ProjectName, name, sha);
         }
         catch (OperatorException ex)
         {
            if (ex.InnerException is GitLabSharp.GitLabClientCancelled)
            {
               return null;
            }
            throw new RepositoryManagerException("Cannot create a new branch", ex);
         }
      }

      async public Task DeleteBranchAsync(ProjectKey projectKey, string name)
      {
         _operator = new RepositoryOperator(projectKey.HostName,
            _settings.GetAccessToken(projectKey.HostName));
         try
         {
            await _operator.DeleteBranchAsync(projectKey.ProjectName, name);
         }
         catch (OperatorException ex)
         {
            if (ex.InnerException is GitLabSharp.GitLabClientCancelled)
            {
               return;
            }
            throw new RepositoryManagerException("Cannot delete a branch", ex);
         }
      }

      async public Task CancelAsync()
      {
         if (_operator != null)
         {
            await _operator.CancelAsync();
         }
      }

      private readonly IHostProperties _settings;
      private RepositoryOperator _operator;
   }
}

