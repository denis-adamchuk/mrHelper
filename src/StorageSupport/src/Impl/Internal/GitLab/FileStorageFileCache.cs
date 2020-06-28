using System.Diagnostics;
using System.IO;

namespace mrHelper.StorageSupport
{
   internal class FileStorageFileCache
   {
      internal FileStorageFileCache(string path)
      {
         Path = path;
      }

      public bool ContainsFileRevision(FileRevision fileRevision)
      {
         return doesFileRevisionExist(fileRevision);
      }

      public string GetFileRevisionPath(FileRevision fileRevision)
      {
         if (!doesFileRevisionExist(fileRevision))
         {
            return string.Empty;
         }

         return getFileRevisionPath(fileRevision);
      }

      public void WriteFileRevision(FileRevision fileRevision, string content)
      {
         writeFileRevision(fileRevision, content);
      }

      public string Path { get; }

      private bool doesFileRevisionExist(FileRevision fileRevision)
      {
         return File.Exists(getFileRevisionPath(fileRevision));
      }

      public void writeFileRevision(FileRevision fileRevision, string content)
      {
         string fileRevisionPath = getFileRevisionPath(fileRevision);
         string fileRevisionDirName = System.IO.Path.GetDirectoryName(fileRevisionPath);
         if (!Directory.Exists(fileRevisionDirName))
         {
            Directory.CreateDirectory(fileRevisionDirName);
         }

         System.IO.File.WriteAllText(fileRevisionPath, content);
      }

      private string getFileRevisionPath(FileRevision fileRevision)
      {
         string prefix = System.IO.Path.Combine(Path, fileRevision.SHA);
         return fileRevision.GitFilePath.ToDiskPath(prefix);
      }
   }
}

