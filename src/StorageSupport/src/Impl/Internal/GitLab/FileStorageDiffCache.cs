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
         _path = Path.Combine(path, DiffSubFolderName);
         _fileStorage = fileStorage;

         cleanupOldDiffs();
      }

      internal FileStorageDiffCacheFolder GetDiffFolder(string baseSha, string headSha)
      {
         return getExistingDiffFolder(baseSha, headSha) ?? createDiffFolder(baseSha, headSha);
      }

      private FileStorageDiffCacheFolder getExistingDiffFolder(string baseSha, string headSha)
      {
         string indexedDir = _index.GetDirectory(baseSha, headSha);
         if (String.IsNullOrEmpty(indexedDir))
         {
            return null;
         }

         string diffFolderPath = Path.Combine(_path, indexedDir);
         if (!Directory.Exists(diffFolderPath) || !verifyDiffFolder(baseSha, headSha, diffFolderPath))
         {
            Trace.TraceWarning("[FileStorageDiffCache] Detected invalid diff folder at path \"{0}\"", diffFolderPath);
            FileStorageUtils.DeleteDirectoryIfExists(diffFolderPath);
            _index.RemoveDirectory(baseSha, headSha);
            return null;
         }
         return new FileStorageDiffCacheFolder(diffFolderPath);
      }

      private bool verifyDiffFolder(string baseSha, string headSha, string diffFolderPath)
      {
         FileStorageDiffCacheFolder diffFolder = new FileStorageDiffCacheFolder(diffFolderPath);
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
         string diffFolderName = cookDirectoryName();
         string diffFolderPath = Path.Combine(_path, diffFolderName);
         string tempDiffFolderPath = System.IO.Path.Combine(_path, "temp");
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
         _index.AddDirectory(baseSha, headSha, diffFolderName);
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
         FileStorageUtils.DeleteDirectoryIfExists(diffFolderPath);

         try
         {
            Directory.Move(tempDiffFolderPath, diffFolderPath);
         }
         catch (Exception ex) // Any exception from Directory.Move()
         {
            throw new FileStorageDiffCacheException(String.Format(
               "Cannot rename a temp folder {0} to {1}", tempDiffFolderPath, diffFolderPath), ex);
         }
      }

      private static void createTempFolders(string tempDiffFolderPath, string tempDiffLeftSubFolderPath,
         string tempDiffRightSubFolderPath)
      {
         FileStorageUtils.DeleteDirectoryIfExists(tempDiffFolderPath);

         try
         {
            Directory.CreateDirectory(tempDiffFolderPath);
            Directory.CreateDirectory(tempDiffLeftSubFolderPath);
            Directory.CreateDirectory(tempDiffRightSubFolderPath);
         }
         catch (Exception ex) // Any exception from Directory.CreateDirectory()
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
            catch (Exception ex) // Any exception from System.IO.Path.GetDirectoryName()
                                 // or System.IO.Directory.CreateDirectory()
                                 // or System.IO.File.Copy() exception
            {
               throw new FileStorageDiffCacheException(String.Format(
                  "Cannot copy file revision {0} to {1}", sourceFilePath, destFilePath), ex);
            }
         }
      }

      private string getFileRevisionPath(FileRevision fileRevision)
      {
         return _fileStorage.FileCache.GetFileRevisionPath(fileRevision);
      }

      private void cleanupOldDiffs()
      {
         FileStorageUtils.DeleteDirectoryIfExists(_path);
      }

      private string cookDirectoryName()
      {
         int index = 1;
         string cookName() => String.Format("d{0:00}", index);
         while (Directory.Exists(Path.Combine(_path, cookName())))
         {
            ++index;
         }
         return cookName();
      }

      private class DirectoryIndex
      {
         internal string GetDirectory(string baseSha, string headSha)
         {
            return _data.TryGetValue(getIndexKey(baseSha, headSha), out string value) ? value : String.Empty;
         }

         internal void RemoveDirectory(string baseSha, string headSha)
         {
            _data.Remove(getIndexKey(baseSha, headSha));
         }

         internal void AddDirectory(string baseSha, string headSha, string dir)
         {
            _data[getIndexKey(baseSha, headSha)] = dir;
         }

         private static Tuple<string, string> getIndexKey(string baseSha, string headSha)
         {
            return new Tuple<string, string>(baseSha, headSha);
         }

         private readonly Dictionary<Tuple<string, string>, string> _data =
            new Dictionary<Tuple<string, string>, string>();
      }

      private readonly string _path;
      private readonly IFileStorage _fileStorage;
      private readonly DirectoryIndex _index = new DirectoryIndex();

      private readonly string DiffSubFolderName = "diff";
   }
}

