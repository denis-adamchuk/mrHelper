using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

namespace mrHelper.StorageSupport
{
   internal class FileStorageDiffCache
   {
      internal FileStorageDiffCache(string path, IFileStorage fileStorage)
      {
         _path = path;
         _fileStorage = fileStorage;

         cleanupOldDiffs();
      }

      internal FileStorageDiffCacheFolder GetDiffFolder(string baseSha, string headSha)
      {
         if (getExistingDiffFolder(baseSha, headSha, out FileStorageDiffCacheFolder diffFolder))
         {
            return diffFolder;
         }
         return createDiffFolder(baseSha, headSha);
      }

      private bool getExistingDiffFolder(string baseSha, string headSha, out FileStorageDiffCacheFolder diffFolder)
      {
         string diffFolderPath = getDiffFolderPath(baseSha, headSha);
         string diffLeftSubFolderPath = System.IO.Path.Combine(diffFolderPath, "left");
         string diffRightSubFolderPath = System.IO.Path.Combine(diffFolderPath, "right");
         if (Directory.Exists(diffFolderPath))
         {
            if (Directory.Exists(diffLeftSubFolderPath) && Directory.Exists(diffRightSubFolderPath))
            {
               diffFolder = new FileStorageDiffCacheFolder(diffFolderPath);
               return true;
            }
            Directory.Delete(diffFolderPath, true);
         }

         diffFolder = null;
         return false;
      }

      private FileStorageDiffCacheFolder createDiffFolder(string baseSha, string headSha)
      {
         string diffFolderPath = getDiffFolderPath(baseSha, headSha);
         string tempDiffFolderPath = getDiffFolderPath("~" + baseSha, headSha);
         if (Directory.Exists(tempDiffFolderPath))
         {
            Directory.Delete(tempDiffFolderPath, true);
         }
         string tempDiffLeftSubFolderPath = System.IO.Path.Combine(tempDiffFolderPath, "left");
         string tempDiffRightSubFolderPath = System.IO.Path.Combine(tempDiffFolderPath, "right");
         Directory.CreateDirectory(tempDiffFolderPath);
         Directory.CreateDirectory(tempDiffLeftSubFolderPath);
         Directory.CreateDirectory(tempDiffRightSubFolderPath);

         GitLabSharp.Entities.Comparison comparison = _fileStorage.ComparisonCache.LoadComparison(baseSha, headSha);
         if (comparison == null)
         {
            Trace.TraceWarning(String.Format(
               "[FileStorageDiffCache] Cannot find a Comparison object. BaseSHA={0}, HeadSHA={1}", baseSha, headSha));
            return null;
         }

         copyFiles(FileStorageUtils.CreateFileRevisions(comparison.Diffs, baseSha, true), tempDiffLeftSubFolderPath);
         copyFiles(FileStorageUtils.CreateFileRevisions(comparison.Diffs, headSha, false), tempDiffRightSubFolderPath);
         Directory.Move(tempDiffFolderPath, diffFolderPath);
         return new FileStorageDiffCacheFolder(diffFolderPath);
      }

      private void copyFiles(IEnumerable<FileRevision> revisions, string tempDiffFolderPath)
      {
         foreach (FileRevision revision in revisions)
         {
            if (!_fileStorage.FileCache.ContainsFileRevision(revision))
            {
               Trace.TraceWarning(String.Format(
                  "[FileStorageDiffCache] Cannot find a file revision. SHA={0}, GitFilePath={1}",
                  revision.SHA, revision.GitFilePath.Value));
               continue;
            }
            string sourceFilePath = getFileRevisionPath(revision);
            string destFilePath = revision.GitFilePath.ToDiskPath(tempDiffFolderPath);
            string subfolder = System.IO.Path.GetDirectoryName(destFilePath);
            if (!Directory.Exists(subfolder))
            {
               Directory.CreateDirectory(subfolder);
            }

            System.IO.File.Copy(sourceFilePath, destFilePath);
         }
      }

      private string getDiffFolderPath(string baseSha, string headSha)
      {
         string diffFolderName = String.Format("{0}_{1}", baseSha, headSha);
         return System.IO.Path.Combine(_path, diffFolderName);
      }

      private string getFileRevisionPath(FileRevision fileRevision)
      {
         return _fileStorage.FileCache.GetFileRevisionPath(fileRevision);
      }

      private void cleanupOldDiffs()
      {
         if (Directory.Exists(_path))
         {
            Directory.Delete(_path, true);
         }
      }

      private readonly string _path;
      private readonly IFileStorage _fileStorage;
   }
}

