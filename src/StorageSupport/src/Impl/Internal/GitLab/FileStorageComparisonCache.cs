using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Common.Tools;
using mrHelper.Common.Exceptions;

namespace mrHelper.StorageSupport
{
   internal class FileStorageComparisonCache
   {
      internal FileStorageComparisonCache(string path, int comparisonsToKeep)
      {
         _path = Path.Combine(path, ComparisonCacheSubFolderName);
         string oldPath = Path.Combine(path, OldComparisonCacheSubFolderName);
         FileStorageUtils.MigrateDirectory(oldPath, _path);

         cleanupOldComparisons(comparisonsToKeep);
      }

      internal Comparison LoadComparison(string baseSha, string headSha)
      {
         string comparisonCacheFilepath = getComparisonCacheFilepath(baseSha, headSha);
         if (System.IO.File.Exists(comparisonCacheFilepath))
         {
            try
            {
               return JsonUtils.LoadFromFile<Comparison>(comparisonCacheFilepath);
            }
            catch (Exception ex)
            {
               ExceptionHandlers.Handle("Cannot read serialized Comparison object", ex);
            }
         }
         return null;
      }

      internal void SaveComparison(string baseSha, string headSha, Comparison comparison)
      {
         string comparisonCacheFilepath = getComparisonCacheFilepath(baseSha, headSha);
         string comparisonCacheDirName = Path.GetDirectoryName(comparisonCacheFilepath);
         if (!System.IO.Directory.Exists(comparisonCacheDirName))
         {
            System.IO.Directory.CreateDirectory(comparisonCacheDirName);
         }

         try
         {
            JsonUtils.SaveToFile(comparisonCacheFilepath, comparison);
         }
         catch (Exception ex)
         {
            ExceptionHandlers.Handle("Cannot serialize Comparison object", ex);
         }
      }

      private string getComparisonCacheFilepath(string baseSha, string headSha)
      {
         string comparisonCacheFileName = String.Format("{0}_{1}.json", baseSha, headSha);
         return Path.Combine(_path, comparisonCacheFileName);
      }

      private void cleanupOldComparisons(int comparisonsToKeep)
      {
         if (!Directory.Exists(_path))
         {
            return;
         }

         IEnumerable<string> allFiles = null;
         try
         {
            allFiles = Directory.GetFiles(_path);
         }
         catch (Exception ex)
         {
            ExceptionHandlers.Handle(String.Format("Cannot obtain a list of comparisons at {0}", _path), ex);
            return;
         }

         IEnumerable<string> filesToBeDeleted =
            allFiles
            .OrderByDescending(x => System.IO.File.GetLastAccessTime(x))
            .Skip(comparisonsToKeep);
         foreach (string file in filesToBeDeleted)
         {
            try
            {
               System.IO.File.Delete(file);
            }
            catch (Exception ex)
            {
               ExceptionHandlers.Handle(String.Format("Cannot delete old comparison {0}", file), ex);
            }
         }
      }

      private readonly string _path;

      private readonly string OldComparisonCacheSubFolderName = "comparison";
      private readonly string ComparisonCacheSubFolderName = "cmp";
   }
}

