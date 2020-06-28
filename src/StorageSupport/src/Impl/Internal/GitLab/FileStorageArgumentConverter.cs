using System;
using System.Linq;
using System.Diagnostics;
using mrHelper.Common.Tools;
using mrHelper.Common.Constants;
using GitLabSharp.Entities;

namespace mrHelper.StorageSupport
{
   class FileStorageArgumentConverter : IGitCommandArgumentConverter
   {
      internal FileStorageArgumentConverter(IFileStorage fileStorage)
      {
         _fileStorage = fileStorage;
      }

      public ConvertedArguments Convert(GitDiffArguments arguments)
      {
         string baseSha = arguments.CommonArgs.Sha1;
         string headSha = arguments.CommonArgs.Sha2;
         throwOnEmptySha(new[] { baseSha, headSha });
         throwOnBadFilenamePair(arguments.CommonArgs.Filename1, arguments.CommonArgs.Filename2);

         Comparison comparison = getComparison(baseSha, headSha);
         createEmptyPlaceHoldersForGitDiff(arguments, comparison);

         string filename1 = getFilePath(baseSha, arguments.CommonArgs.Filename1);
         string filename2 = getFilePath(headSha, arguments.CommonArgs.Filename2);

         GitDiffArguments modifiedArguments = new GitDiffArguments(arguments.Mode,
            new GitDiffArguments.CommonArguments(string.Empty, string.Empty, filename1, filename2,
            arguments.CommonArgs.Filter), arguments.SpecialArgs);
         return new ConvertedArguments("git", modifiedArguments.ToString());
      }

      public ConvertedArguments Convert(DiffToolArguments arguments)
      {
         string baseSha = arguments.LeftSHA;
         string headSha = arguments.RightSHA;
         throwOnEmptySha(new[] { baseSha, headSha });

         FileStorageDiffCacheFolder diffFolder = _fileStorage.DiffCache.GetDiffFolder(baseSha, headSha);
         if (diffFolder == null || diffFolder.LeftSubfolder == null || diffFolder.RightSubfolder == null)
         {
            throw new ArgumentConversionException("Cannot locate or create a folder with diff", null);
         }

         var configValue = GitTools.GetConfigKeyValue(GitTools.ConfigScope.Global, Constants.GitDiffToolConfigKey);
         if (String.IsNullOrEmpty(configValue.FirstOrDefault()))
         {
            throw new ArgumentConversionException("Diff Tool is not registered", null);
         }

         string diffToolPath = configValue.FirstOrDefault()
            .Replace("$LOCAL", diffFolder.LeftSubfolder)
            .Replace("$REMOTE", diffFolder.RightSubfolder)
            .Replace("//", "/");
         return new ConvertedArguments(diffToolPath, String.Empty);
      }

      private string getFilePath(string sha, string gitFilepath)
      {
         if (String.IsNullOrEmpty(gitFilepath))
         {
            return String.Empty;
         }

         FileRevision fileRevision = new FileRevision(gitFilepath, sha);
         return _fileStorage.FileCache.GetFileRevisionPath(fileRevision);
      }

      private Comparison getComparison(string baseSha, string headSha)
      {
         Comparison comparison = _fileStorage.ComparisonCache.LoadComparison(baseSha, headSha);
         if (comparison == null)
         {
            Trace.TraceWarning(String.Format(
               "[FileStorageArgumentConverter] Cannot find a Comparison object. BaseSHA={0}, HeadSHA={1}",
               baseSha, headSha));
            throw new ArgumentConversionException("Cannot find Comparison object", null);
         }
         return comparison;
      }

      private void createEmptyPlaceHoldersForGitDiff(GitDiffArguments arguments, Comparison comparison)
      {
         foreach (GitLabSharp.Entities.DiffStruct diff in comparison.Diffs)
         {
            if (diff.Old_Path == arguments.CommonArgs.Filename1
             && diff.New_Path == arguments.CommonArgs.Filename2)
            {
               Debug.Assert(!String.IsNullOrEmpty(diff.Old_Path));
               Debug.Assert(!String.IsNullOrEmpty(diff.New_Path));

               if (diff.New_File)
               {
                  createEmptyRevision(arguments.CommonArgs.Sha1, diff.Old_Path);
               }
               else if (diff.Deleted_File)
               {
                  createEmptyRevision(arguments.CommonArgs.Sha2, diff.New_Path);
               }
            }
         }
      }

      private void createEmptyRevision(string sha, string gitFilepath)
      {
         FileRevision fileRevision = new FileRevision(gitFilepath, sha);
         if (!_fileStorage.FileCache.ContainsFileRevision(fileRevision))
         {
            _fileStorage.FileCache.WriteFileRevision(fileRevision, String.Empty);
         }
      }

      private static void throwOnEmptySha(string[] sha)
      {
         if (sha.Any(x => String.IsNullOrEmpty(x)))
         {
            throw new ArgumentConversionException("Bad SHA", null);
         }
      }

      private static void throwOnBadFilenamePair(string filename1, string filename2)
      {
         bool isFilename1Empty = String.IsNullOrEmpty(filename1);
         bool isFilename2Empty = String.IsNullOrEmpty(filename2);
         if (isFilename1Empty != isFilename2Empty)
         {
            throw new ArgumentConversionException("Bad filename pair", null);
         }
      }

      private readonly IFileStorage _fileStorage;
   }
}

