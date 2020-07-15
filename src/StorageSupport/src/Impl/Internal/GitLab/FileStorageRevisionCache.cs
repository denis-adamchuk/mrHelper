using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace mrHelper.StorageSupport
{
   internal class FileStorageRevisionCache
   {
      internal FileStorageRevisionCache(string path, int revisionsToKeep)
      {
         _path = path;

         cleanupOldRevisions(revisionsToKeep);
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
         string fileRevisionDirName = System.IO.Path.GetDirectoryName(fileRevisionPath);
         if (!Directory.Exists(fileRevisionDirName))
         {
            Directory.CreateDirectory(fileRevisionDirName);
         }

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

      // From https://git.kernel.org/pub/scm/git/git.git/tree/xdiff-interface.c#n187
      private static int FirstFewBytes = 8000;
      private static bool isBinaryData(byte[] data)
      {
         return data.Take(FirstFewBytes).Any(x => x == 0);
      }

      private string getFileRevisionPath(FileRevision fileRevision)
      {
         string prefix = System.IO.Path.Combine(_path, fileRevision.SHA);
         return fileRevision.GitFilePath.ToDiskPath(prefix);
      }

      private void cleanupOldRevisions(int revisionsToKeep)
      {
         if (!Directory.Exists(_path))
         {
            return;
         }

         IEnumerable<string> allSubdirectories = Directory.GetDirectories(_path, "*", SearchOption.TopDirectoryOnly);
         IEnumerable<string> subdirectoriesToBeDeleted =
            allSubdirectories
            .OrderByDescending(x => Directory.GetLastAccessTime(x))
            .Skip(revisionsToKeep);
         foreach (string directory in subdirectoriesToBeDeleted)
         {
            Directory.Delete(directory, true);
         }
      }

      private string _path;
   }
}

