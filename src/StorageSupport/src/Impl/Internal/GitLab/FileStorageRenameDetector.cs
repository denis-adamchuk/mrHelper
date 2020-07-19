using System;

namespace mrHelper.StorageSupport
{
   internal class FileStorageRenameDetector : IFileRenameDetector
   {
      internal FileStorageRenameDetector(IGitCommandService commandService, IFileStorage fileStorage)
      {
         _commandService = commandService;
         _comparisonCache = fileStorage.ComparisonCache;
         _fileCache = fileStorage.FileCache;
      }

      public string IsRenamed(string leftcommit, string rightcommit, string filename, bool leftsidename,
         out bool moved)
      {
         GitLabSharp.Entities.Comparison comparison = _comparisonCache.LoadComparison(leftcommit, rightcommit);
         if (comparison == null)
         {
            throw new FileRenameDetectorException(String.Format(
               "Cannot find Comparison object for {0} vs {1}", leftcommit, rightcommit), null);
         }

         try
         {
            return isRenamed(leftcommit, rightcommit, filename, leftsidename, out moved, comparison);
         }
         catch (Exception ex)
         {
            throw new FileRenameDetectorException(String.Format(
               "Cannot process Comparison object for {0} vs {1}", leftcommit, rightcommit), ex);
         }
      }

      private string isRenamed(string leftcommit, string rightcommit, string filename, bool leftsidename, out bool moved,
         GitLabSharp.Entities.Comparison comparison)
      {
         FileRevision fileRevision = new FileRevision(filename, leftsidename ? leftcommit : rightcommit);
         string fileRevisionPath = _fileCache.GetFileRevisionPath(fileRevision);
         string fileContent = System.IO.File.ReadAllText(fileRevisionPath);

         foreach (GitLabSharp.Entities.DiffStruct diff in comparison.Diffs)
         {
            if (leftsidename && filename == diff.Old_Path)
            {
               FileRevision oppositeRevision = new FileRevision(diff.New_Path, rightcommit);
               string oppositeRevisionPath = _fileCache.GetFileRevisionPath(oppositeRevision);
               string oppositeFileContent = oppositeRevisionPath == String.Empty ?
                  String.Empty : System.IO.File.ReadAllText(oppositeRevisionPath);
               moved = fileContent == oppositeFileContent && diff.Old_Path != diff.New_Path;
               return diff.New_Path;
            }
            else if (!leftsidename && filename == diff.New_Path)
            {
               FileRevision oppositeRevision = new FileRevision(diff.Old_Path, leftcommit);
               string oppositeRevisionPath = _fileCache.GetFileRevisionPath(oppositeRevision);
               string oppositeFileContent = oppositeRevisionPath == String.Empty ?
                  String.Empty : System.IO.File.ReadAllText(oppositeRevisionPath);
               moved = fileContent == oppositeFileContent && diff.Old_Path != diff.New_Path;
               return diff.Old_Path;
            }
         }

         moved = false;
         return filename;
      }

      private readonly IGitCommandService _commandService;
      private readonly FileStorageComparisonCache _comparisonCache;
      private readonly FileStorageRevisionCache _fileCache;
   }
}

