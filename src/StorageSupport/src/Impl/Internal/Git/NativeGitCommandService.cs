using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;

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

      public override int LaunchDiffTool(DiffToolArguments arguments)
      {
         return ExternalProcess.Start("git", arguments.ToString(), false, _path).PID;
      }

      public override IFileRenameDetector RenameDetector { get; }

      protected override IEnumerable<string> getSync<T>(T arguments)
      {
         return getSyncFromExternalProcess("git", arguments.ToString(), _path, null);
      }

      protected override Task<IEnumerable<string>> getAsync<T>(T arguments)
      {
         return fetchAsyncFromExternalProcess("git", arguments.ToString(), _path, null);
      }

      private readonly string _path;
   }
}

