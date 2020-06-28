using System.Collections.Generic;
using System.Threading.Tasks;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;

namespace mrHelper.StorageSupport
{
   internal class FileStorageGitCommandService : GitCommandService
   {
      internal FileStorageGitCommandService(
         IExternalProcessManager operationManager, string path, IFileStorage fileStorage)
         : base(operationManager)
      {
         _fileCache = fileStorage.FileCache;
         _path = path;
         _argumentConverter = new FileStorageArgumentConverter(fileStorage);
         RenameDetector = new FileStorageRenameDetector(this, fileStorage);
      }

      public override int LaunchDiffTool(DiffToolArguments arguments)
      {
         ConvertedArguments converted = _argumentConverter.Convert(arguments);
         return ExternalProcess.Start(converted.App, converted.Arguments, false, _path).PID;
      }

      public override IFileRenameDetector RenameDetector { get; }

      protected override IEnumerable<string> getSync<T>(T arguments)
      {
         return getSyncTyped((dynamic)arguments);
      }

      protected IEnumerable<string> getSyncTyped(GitDiffArguments arguments)
      {
         ConvertedArguments converted = _argumentConverter.Convert(arguments);
         return getSyncFromExternalProcess(converted.App, converted.Arguments, _path, new int[] { 0, 1 });
      }

      protected IEnumerable<string> getSyncTyped(GitShowRevisionArguments arguments)
      {
         FileRevision fileRevision = new FileRevision(arguments.Filename, arguments.Sha);
         string fileRevisionPath = _fileCache.GetFileRevisionPath(fileRevision);
         return System.IO.File.ReadLines(fileRevisionPath);
      }

      protected override Task<IEnumerable<string>> getAsync<T>(T arguments)
      {
         return getAsyncTyped((dynamic)arguments);
      }

      async protected Task<IEnumerable<string>> getAsyncTyped(GitDiffArguments arguments)
      {
         ConvertedArguments converted = _argumentConverter.Convert(arguments);
         return await fetchAsyncFromExternalProcess(converted.App, converted.Arguments, _path, new int[] { 0, 1 });
      }

      protected Task<IEnumerable<string>> getAsyncTyped(GitShowRevisionArguments arguments)
      {
         FileRevision fileRevision = new FileRevision(arguments.Filename, arguments.Sha);
         string fileRevisionPath = _fileCache.GetFileRevisionPath(fileRevision);
         return Task.FromResult<IEnumerable<string>>(System.IO.File.ReadLines(fileRevisionPath));
      }

      private readonly FileStorageArgumentConverter _argumentConverter;
      private readonly FileStorageFileCache _fileCache;
      private readonly string _path;
   }
}

