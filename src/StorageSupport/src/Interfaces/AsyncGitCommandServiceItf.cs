using System;
using System.Threading.Tasks;
using mrHelper.Common.Exceptions;
using mrHelper.GitLabClient;

namespace mrHelper.StorageSupport
{
   public class FetchFailedException : ExceptionEx
   {
      public FetchFailedException(Exception ex)
         : base(String.Empty, ex)
      {
      }
   }

   /// <summary>
   /// </summary>
   public interface IAsyncGitCommandService : IGitCommandService
   {
      Task FetchAsync(GitShowRevisionArguments arguments);
      Task FetchAsync(GitDiffArguments arguments);
      Task FetchAsync(RevisionComparisonArguments arguments, RepositoryAccessor repositoryAccessor);
   }
}

