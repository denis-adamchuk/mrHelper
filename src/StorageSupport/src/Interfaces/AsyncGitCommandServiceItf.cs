using System;
using System.Threading.Tasks;

namespace mrHelper.StorageSupport
{
   public class FetchFailedException : GitDataException
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
   }
}

