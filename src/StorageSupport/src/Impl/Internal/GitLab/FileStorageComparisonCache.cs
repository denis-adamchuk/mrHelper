using System;
using System.IO;
using GitLabSharp.Entities;
using mrHelper.Common.Tools;
using mrHelper.Common.Exceptions;

namespace mrHelper.StorageSupport
{
   internal class FileStorageComparisonCache
   {
      internal FileStorageComparisonCache(string path)
      {
         _path = path;
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

      private readonly string _path;
   }
}

