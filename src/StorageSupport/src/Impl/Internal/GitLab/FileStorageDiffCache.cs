using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using mrHelper.Common.Exceptions;

namespace mrHelper.StorageSupport
{
   public class FileStorageDiffCacheException : ExceptionEx
   {
      public FileStorageDiffCacheException(string message, Exception ex)
         : base(message, ex)
      {
      }
   }

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
         return getExistingDiffFolder(baseSha, headSha) ?? createDiffFolder(baseSha, headSha);
      }

      private FileStorageDiffCacheFolder getExistingDiffFolder(string baseSha, string headSha)
      {
         string diffFolderPath = getDiffFolderPath(baseSha, headSha);
         if (!Directory.Exists(diffFolderPath))
         {
            return null;
         }

         FileStorageDiffCacheFolder diffFolderCandidate = new FileStorageDiffCacheFolder(diffFolderPath);
         if (!verifyDiffFolder(baseSha, headSha, diffFolderCandidate))
         {
            Trace.TraceWarning(String.Format(
               "[FileStorageDiffCache] Detected invalid diff folder at path \"{0}\"", diffFolderPath));
            try
            {
               Directory.Delete(diffFolderPath, true);
            }
            catch (Exception ex)
            {
               throw new FileStorageDiffCacheException("Cannot delete invalid diff folder", ex);
            }
            return null;
         }
         return diffFolderCandidate;
      }

      private bool verifyDiffFolder(string baseSha, string headSha, FileStorageDiffCacheFolder diffFolder)
      {
         if (!Directory.Exists(diffFolder.LeftSubfolder) || !Directory.Exists(diffFolder.RightSubfolder))
         {
            return false;
         }

         getRevisions(baseSha, headSha, out var baseRevisions, out var headRevisions);
         if (baseRevisions == null || headRevisions == null)
         {
            return false;
         }

         return baseRevisions.Select(x => x.GitFilePath.ToDiskPath(diffFolder.LeftSubfolder)).All(x => File.Exists(x))
             && headRevisions.Select(x => x.GitFilePath.ToDiskPath(diffFolder.RightSubfolder)).All(x => File.Exists(x));
      }

      private FileStorageDiffCacheFolder createDiffFolder(string baseSha, string headSha)
      {
         string diffFolderPath = getDiffFolderPath(baseSha, headSha);
         string tempDiffFolderPath = getDiffFolderPath("~" + baseSha, headSha);
         string tempDiffLeftSubFolderPath = System.IO.Path.Combine(tempDiffFolderPath, "left");
         string tempDiffRightSubFolderPath = System.IO.Path.Combine(tempDiffFolderPath, "right");
         createTempFolders(tempDiffFolderPath, tempDiffLeftSubFolderPath, tempDiffRightSubFolderPath);

         getRevisions(baseSha, headSha, out var baseRevisions, out var headRevisions);
         if (baseRevisions == null || headRevisions == null)
         {
            return null;
         }

         copyFiles(baseRevisions, tempDiffLeftSubFolderPath);
         copyFiles(headRevisions, tempDiffRightSubFolderPath);

         renameTempToPermanentFolder(diffFolderPath, tempDiffFolderPath);
         return new FileStorageDiffCacheFolder(diffFolderPath);
      }

      private void getRevisions(string baseSha, string headSha,
         out IEnumerable<FileRevision> baseRevisions, out IEnumerable<FileRevision> headRevisions)
      {
         GitLabSharp.Entities.Comparison comparison = _fileStorage.ComparisonCache.LoadComparison(baseSha, headSha);
         if (comparison == null)
         {
            Trace.TraceWarning(String.Format(
               "[FileStorageDiffCache] Cannot find a Comparison object. BaseSHA={0}, HeadSHA={1}", baseSha, headSha));
            baseRevisions = null;
            headRevisions = null;
            return;
         }

         baseRevisions = FileStorageUtils.TransformDiffs<FileRevision>(comparison.Diffs, baseSha, true);
         headRevisions = FileStorageUtils.TransformDiffs<FileRevision>(comparison.Diffs, headSha, false);
      }

      private static void renameTempToPermanentFolder(string diffFolderPath, string tempDiffFolderPath)
      {
         try
         {
            Directory.Move(tempDiffFolderPath, diffFolderPath);
         }
         catch (Exception ex)
         {
            throw new FileStorageDiffCacheException(String.Format(
               "Cannot rename a temp folder {0} to {1}", tempDiffFolderPath, diffFolderPath), ex);
         }
      }

      private static void createTempFolders(string tempDiffFolderPath, string tempDiffLeftSubFolderPath,
         string tempDiffRightSubFolderPath)
      {
         if (Directory.Exists(tempDiffFolderPath))
         {
            try
            {
               Directory.Delete(tempDiffFolderPath, true);
            }
            catch (Exception ex)
            {
               throw new FileStorageDiffCacheException(String.Format(
                  "Cannot delete temp diff folder {0}", tempDiffFolderPath), ex);
            }
         }

         try
         {
            Directory.CreateDirectory(tempDiffFolderPath);
            Directory.CreateDirectory(tempDiffLeftSubFolderPath);
            Directory.CreateDirectory(tempDiffRightSubFolderPath);
         }
         catch (Exception ex)
         {
            throw new FileStorageDiffCacheException(String.Format(
               "Cannot create a temp folder {0} or one of its subfolders", tempDiffFolderPath), ex);
         }
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

            try
            {
               string subfolder = System.IO.Path.GetDirectoryName(destFilePath);
               if (!Directory.Exists(subfolder))
               {
                  Directory.CreateDirectory(subfolder);
               }
               System.IO.File.Copy(sourceFilePath, destFilePath, true);
            }
            catch (Exception ex)
            {
               throw new FileStorageDiffCacheException(String.Format(
                  "Cannot copy file revision {0} to {1}", sourceFilePath, destFilePath), ex);
            }
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
         try
         {
            if (Directory.Exists(_path))
            {
               Directory.Delete(_path, true);
            }
         }
         catch (Exception ex)
         {
            ExceptionHandlers.Handle(String.Format("Cannot delete a diff folder {0}", _path), ex);
         }
      }

      private readonly string _path;
      private readonly IFileStorage _fileStorage;
   }
}

