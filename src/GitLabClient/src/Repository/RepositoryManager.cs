using System;
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
         _operator = new RepositoryOperator(settings);
      }

      public Task<Comparison> CompareAsync(ProjectKey projectKey, string from, string to)
      {
         try
         {
            return _operator.CompareAsync(projectKey, from, to);
         }
         catch (OperatorException ex)
         {
            throw new RepositoryManagerException("Cannot perform comparison", ex);
         }
      }

      public Task<File> LoadFileAsync(ProjectKey projectKey, string filename, string sha)
      {
         try
         {
            return _operator.LoadFileAsync(projectKey, filename, sha);
         }
         catch (OperatorException ex)
         {
            throw new RepositoryManagerException("Cannot perform comparison", ex);
         }
      }

      private readonly RepositoryOperator _operator;
   }
}

