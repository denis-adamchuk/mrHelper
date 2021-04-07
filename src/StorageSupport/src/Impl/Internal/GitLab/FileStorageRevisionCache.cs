using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using mrHelper.Common.Exceptions;

namespace mrHelper.StorageSupport
{
   public class FileStorageRevisionCacheException : ExceptionEx
   {
      public FileStorageRevisionCacheException(string message, Exception ex)
         : base(message, ex)
      {
      }
   }

   internal class FileStorageRevisionCache
   {
      internal FileStorageRevisionCache(string path, int revisionsToKeep)
      {
         Path = System.IO.Path.Combine(path, RevisionsSubFolderName);
         string oldPath = System.IO.Path.Combine(path, OldRevisionsSubFolderName);
         FileStorageUtils.MigrateDirectory(oldPath, Path);

         cleanupOldRevisions(revisionsToKeep);
         renameOldRevisions();
      }

      internal bool ContainsFileRevision(FileRevision fileRevision)
      {
         return doesFileRevisionExist(fileRevision);
      }

      internal string GetFileRevisionPath(FileRevision fileRevision)
      {
         if (!doesFileRevisionExist(fileRevision))
         {
            return string.Empty;
         }

         return getFileRevisionPath(fileRevision);
      }

      internal string GetRevisionPath(string sha)
      {
         return getRevisionPath(sha);
      }

      internal string Path { get; }

      internal void WriteFileRevision(string path, string sha, byte[] content)
      {
         writeFileRevision(path, sha, content);
      }

      private bool doesFileRevisionExist(FileRevision fileRevision)
      {
         return File.Exists(getFileRevisionPath(fileRevision));
      }

      public void writeFileRevision(string path, string sha, byte[] content)
      {
         FileRevision fileRevision = new FileRevision(path, sha);
         string fileRevisionPath = getFileRevisionPath(fileRevision);
         try
         {
            string fileRevisionDirName = System.IO.Path.GetDirectoryName(fileRevisionPath);
            if (!Directory.Exists(fileRevisionDirName))
            {
               Directory.CreateDirectory(fileRevisionDirName);
            }
         }
         catch (Exception ex) // Any exceptio Path.GetDirectoryName() or Directory.CreateDirectory()
         {
            throw new FileStorageRevisionCacheException(String.Format(
               "Cannot create a directory for revision {0}", fileRevisionPath), ex);
         }

         try
         {
            bool isBinary = isBinaryData(content);
            if (isBinary)
            {
               File.WriteAllBytes(fileRevisionPath, content);
            }
            else
            {
               string contentAsString = System.Text.Encoding.UTF8.GetString(content);
               contentAsString = Common.Tools.StringUtils.ConvertNewlineUnixToWindows(contentAsString);
               File.WriteAllText(fileRevisionPath, contentAsString);
            }
         }
         catch (Exception ex) // Any exception from File or System.Text.Encoding.UTF8.GetString() or I/O operations
         {
            throw new FileStorageRevisionCacheException(String.Format(
               "Cannot write a file revision at {0}", fileRevisionPath), ex);
         }
      }

      // From https://git.kernel.org/pub/scm/git/git.git/tree/xdiff-interface.c#n187
      private static readonly int FirstFewBytes = 8000;
      private static bool isBinaryData(byte[] data)
      {
         return data.Take(FirstFewBytes).Any(x => x == 0);
      }

      private string getFileRevisionPath(FileRevision fileRevision)
      {
         string prefix = getRevisionPath(fileRevision.SHA);
         return fileRevision.GitFilePath.ToDiskPath(prefix);
      }

      private string getRevisionPath(string sha)
      {
         return System.IO.Path.Combine(Path, FileStorageUtils.ConvertShaToRevision(sha));
      }

      private void cleanupOldRevisions(int revisionsToKeep)
      {
         if (!Directory.Exists(Path))
         {
            return;
         }

         IEnumerable<string> allSubdirectories = null;
         try
         {
            allSubdirectories = Directory.GetDirectories(Path, "*", SearchOption.TopDirectoryOnly);
         }
         catch (Exception ex) // Any exception from Directory.GetDirectories()
         {
            ExceptionHandlers.Handle(String.Format("Cannot obtain a list of subdirectories at {0}", Path), ex);
            return;
         }

         IEnumerable<string> subdirectoriesToBeDeleted =
            allSubdirectories
            .OrderByDescending(x => Directory.GetLastAccessTime(x))
            .Skip(revisionsToKeep);
         foreach (string directory in subdirectoriesToBeDeleted)
         {
            FileStorageUtils.DeleteDirectoryIfExists(directory);
         }
      }

      private void renameOldRevisions()
      {
         if (!Directory.Exists(Path))
         {
            return;
         }

         IEnumerable<string> allSubdirectories;
         try
         {
            allSubdirectories = Directory.GetDirectories(Path, "*", SearchOption.TopDirectoryOnly);
         }
         catch (Exception ex) // Any exception from Directory.GetDirectories()
         {
            ExceptionHandlers.Handle(String.Format("Cannot obtain a list of subdirectories at {0}", Path), ex);
            return;
         }

         foreach (string oldPath in allSubdirectories)
         {
            string oldRevisionDirName;
            try
            {
               oldRevisionDirName = System.IO.Path.GetFileName(oldPath);
            }
            catch (ArgumentException ex)
            {
               ExceptionHandlers.Handle(String.Format("Cannot obtain directory name from path {0}", oldPath), ex);
               continue;
            }
            if (oldRevisionDirName.Length != FileStorageUtils.FullShaLength)
            {
               continue;
            }

            // oldRevisionDirName is a full git SHA
            string newRevisionDirName = FileStorageUtils.ConvertShaToRevision(oldRevisionDirName);
            string newPath = System.IO.Path.Combine(Path, newRevisionDirName);
            FileStorageUtils.MigrateDirectory(oldPath, newPath);
         }
      }

      private readonly string OldRevisionsSubFolderName = "revisions";
      private readonly string RevisionsSubFolderName = "rev";
   }
}

