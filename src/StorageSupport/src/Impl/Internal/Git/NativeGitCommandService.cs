using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient;

namespace mrHelper.StorageSupport
{
   class NativeGitCommandService : GitCommandService
   {
      internal NativeGitCommandService(IExternalProcessManager operationManager, string path)
         : base(operationManager)
      {
         _path = path;
         RenameDetector = new GitRepositoryRenameDetector(this);
      }

      public override IFileRenameDetector RenameDetector { get; }

      protected override object runCommand(GitDiffArguments arguments)
      {
         return startExternalProcess("git", arguments.ToString(), _path, true, null).StdOut;
      }

      protected override object runCommand(GitShowRevisionArguments arguments)
      {
         return startExternalProcess("git", arguments.ToString(), _path, true, null).StdOut;
      }

      protected override object runCommand(DiffToolArguments arguments)
      {
         return startExternalProcess("git", arguments.ToString(), _path, false, null).PID;
      }

      async protected override Task<object> runCommandAsync(GitDiffArguments arguments)
      {
         return (await startExternalProcessAsync("git", arguments.ToString(), _path, null)).StdOut;
      }

      async protected override Task<object> runCommandAsync(GitShowRevisionArguments arguments)
      {
         return (await startExternalProcessAsync("git", arguments.ToString(), _path, null)).StdOut;
      }

      async protected override Task<object> runCommandAsync(
         RevisionComparisonArguments arguments, RepositoryAccessor repositoryAccessor)
      {
         try
         {
            Comparison comparison = await repositoryAccessor.Compare(arguments.Sha1, arguments.Sha2, null);
            return comparison == null ? null : new ComparisonEx(comparison);
         }
         catch (RepositoryAccessorException ex)
         {
            throw new GitCommandServiceInternalException(ex);
         }
      }

      private readonly string _path;
   }
}

