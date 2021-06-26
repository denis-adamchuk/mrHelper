using System.Threading.Tasks;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient;

namespace mrHelper.StorageSupport
{
   class NativeGitCommandService : GitCommandService
   {
      internal NativeGitCommandService(IExternalProcessManager operationManager, string path,
         RepositoryAccessor repositoryAccessor)
         : base(operationManager, repositoryAccessor)
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

      async protected override Task<object> runCommandAsync(RevisionComparisonArguments arguments)
      {
         return await _repositoryAccessor.Compare(arguments.Sha1, arguments.Sha2, null);
      }

      private readonly string _path;
   }
}

