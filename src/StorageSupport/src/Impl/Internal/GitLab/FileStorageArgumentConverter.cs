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
         createDummyPlaceHoldersForGitDiff(arguments, comparison, out bool dummyOld, out bool dummyNew);

         string filename1;
         string filename2;
         switch (arguments.Mode)
         {
            case GitDiffArguments.DiffMode.Context:
               filename1 = getFilePath(baseSha, arguments.CommonArgs.Filename1, dummyOld);
               filename2 = getFilePath(headSha, arguments.CommonArgs.Filename2, dummyNew);
               break;

            case GitDiffArguments.DiffMode.ShortStat:
               filename1 = getPath(baseSha);
               filename2 = getPath(headSha);
               break;

            case GitDiffArguments.DiffMode.NumStat:
               // Not tested.
               // NumStat Mode is used to detect renames in GitRepositoryRenameDetector.
               // FileStorageRenameDetector does not use it.

            default:
               throw new NotImplementedException();
         }

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

         FileStorageDiffCacheFolder diffFolder = getDiffFolder(baseSha, headSha);
         Debug.Assert(diffFolder != null);

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

      private FileStorageDiffCacheFolder getDiffFolder(string baseSha, string headSha)
      {
         FileStorageDiffCacheFolder diffFolder;
         try
         {
            diffFolder = _fileStorage.DiffCache.GetDiffFolder(baseSha, headSha);
         }
         catch (FileStorageDiffCacheException ex)
         {
            throw new ArgumentConversionException(String.Format(
               "Cannot locate or create a folder with diff for {0} vs {1}", baseSha, headSha), ex);
         }
         if (diffFolder == null || diffFolder.LeftSubfolder == null || diffFolder.RightSubfolder == null)
         {
            throw new ArgumentConversionException(String.Format(
               "Invalid diff folder for {0} vs {1}", baseSha, headSha), null);
         }
         return diffFolder;
      }

      private string getFilePath(string sha, string gitFilepath, bool isDummy)
      {
         if (String.IsNullOrEmpty(gitFilepath))
         {
            return String.Empty;
         }

         FileRevision fileRevision = new FileRevision(gitFilepath + (isDummy ? DummyRevisionSuffix : ""), sha);
         return _fileStorage.FileCache.GetFileRevisionPath(fileRevision);
      }

      private string getPath(string sha)
      {
         return _fileStorage.FileCache.GetRevisionPath(sha);
      }

      private Comparison getComparison(string baseSha, string headSha)
      {
         Comparison comparison = _fileStorage.ComparisonCache.LoadComparison(baseSha, headSha);
         if (comparison == null)
         {
            throw new ArgumentConversionException(String.Format(
               "Cannot find Comparison object for {0} vs {1}", baseSha, headSha), null);
         }
         return comparison;
      }

      private void createDummyPlaceHoldersForGitDiff(GitDiffArguments arguments, Comparison comparison,
         out bool dummyOld, out bool dummyNew)
      {
         dummyOld = false;
         dummyNew = false;

         foreach (DiffStruct diff in comparison.Diffs)
         {
            if (diff.Old_Path == arguments.CommonArgs.Filename1 && diff.New_Path == arguments.CommonArgs.Filename2)
            {
               Debug.Assert(!String.IsNullOrEmpty(diff.Old_Path) && !String.IsNullOrEmpty(diff.New_Path));
               if (diff.New_File)
               {
                  dummyOld = createDummyRevision(arguments.CommonArgs.Sha1, diff.Old_Path);
                  dummyNew = false;
               }
               else if (diff.Deleted_File)
               {
                  dummyOld = false;
                  dummyNew = createDummyRevision(arguments.CommonArgs.Sha2, diff.New_Path);
               }
               return;
            }
         }
      }

      private bool createDummyRevision(string sha, string gitFilepath)
      {
         FileRevision fileRevision = new FileRevision(gitFilepath, sha);
         if (!_fileStorage.FileCache.ContainsFileRevision(fileRevision))
         {
            try
            {
               _fileStorage.FileCache.WriteFileRevision(gitFilepath + DummyRevisionSuffix, sha, Array.Empty<byte>());
               return true;
            }
            catch (FileStorageRevisionCacheException ex)
            {
               throw new ArgumentConversionException(String.Format("Cannot create a dummy revision for {0}",
                  gitFilepath), ex);
            }
         }
         return false;
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
      private static readonly string DummyRevisionSuffix = "__mrhelper_dummy_file_revision";
   }
}

