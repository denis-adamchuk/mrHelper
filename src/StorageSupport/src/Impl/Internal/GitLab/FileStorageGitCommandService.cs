using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;
using mrHelper.GitLabClient;

namespace mrHelper.StorageSupport
{
   internal class FileStorageGitCommandService : GitCommandService
   {
      internal FileStorageGitCommandService(
         IExternalProcessManager operationManager, string path, IFileStorage fileStorage)
         : base(operationManager)
      {
         _fileCache = fileStorage.FileCache;
         _comparisonCache = fileStorage.ComparisonCache;
         _path = path;
         _argumentConverter = new FileStorageArgumentConverter(fileStorage);
         RenameDetector = new FileStorageRenameDetector(fileStorage);
      }

      public override IFileRenameDetector RenameDetector { get; }

      protected override object runCommand(GitDiffArguments arguments)
      {
         try
         {
            ConvertedArguments converted = _argumentConverter.Convert(arguments);
            return startExternalProcess(converted.App, converted.Arguments, _path, true, new int[] { 0, 1 })
               .StdOut.Where(x => !String.IsNullOrEmpty(x));
         }
         catch (ArgumentConversionException ex)
         {
            throw new GitCommandServiceInternalException(ex);
         }
      }

      protected override object runCommand(GitShowRevisionArguments arguments)
      {
         FileRevision fileRevision = new FileRevision(arguments.Filename, arguments.Sha);
         string fileRevisionPath = _fileCache.GetFileRevisionPath(fileRevision);
         string content;
         try
         {
            content = System.IO.File.ReadAllText(fileRevisionPath);
         }
         catch (Exception ex) // Any exception from System.IO.File.ReadAllText()
         {
            throw new GitCommandServiceInternalException(ex);
         }
         return StringUtils.ConvertNewlineWindowsToUnix(content).Split('\n');
      }

      protected override object runCommand(DiffToolArguments arguments)
      {
         try
         {
            ConvertedArguments converted = _argumentConverter.Convert(arguments);
            return startExternalProcess(converted.App, converted.Arguments, _path, false, null).PID;
         }
         catch (ArgumentConversionException ex)
         {
            throw new GitCommandServiceInternalException(ex);
         }
      }

      async protected override Task<object> runCommandAsync(GitDiffArguments arguments)
      {
         try
         {
            ConvertedArguments converted = _argumentConverter.Convert(arguments);
            IEnumerable<string> result =
               (await startExternalProcessAsync(converted.App, converted.Arguments, _path, new int[] { 0, 1 })).StdOut;
            return result.Where(x => !String.IsNullOrEmpty(x));
         }
         catch (ArgumentConversionException ex)
         {
            throw new GitCommandServiceInternalException(ex);
         }
      }

      protected override Task<object> runCommandAsync(GitShowRevisionArguments arguments)
      {
         return Task.FromResult<object>(runCommand(arguments));
      }

      async protected override Task<object> runCommandAsync(
         RevisionComparisonArguments arguments, RepositoryAccessor repositoryAccessor)
      {
         Comparison comparison = await repositoryAccessor.Compare(arguments.Sha1, arguments.Sha2, _comparisonCache);
         return comparison == null ? null : new ComparisonEx(comparison);
      }

      private readonly FileStorageArgumentConverter _argumentConverter;
      private readonly FileStorageRevisionCache _fileCache;
      private readonly FileStorageComparisonCache _comparisonCache;
      private readonly string _path;
   }
}

