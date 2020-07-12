using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrHelper.StorageSupport
{
   internal class FileStorageFullContextDiffProvider : IFullContextDiffProvider
   {
      internal FileStorageFullContextDiffProvider(IGitCommandService commandService,
         FileStorageComparisonCache comparisonCache)
      {
         _commandService = commandService;
         _comparisonCache = comparisonCache;
      }

      public FullContextDiff GetFullContextDiff(string leftSHA, string rightSHA,
         string leftFileName, string rightFileName)
      {
         FileRevision leftRevision = new FileRevision(leftSHA, leftFileName);
         string leftRevisionPath = _fileCache.GetFileRevisionPath(leftRevision);
         if (String.IsNullOrEmpty(leftRevisionPath))
         {
            throw new FullContextDiffProviderException("Cannot obtain left file revision path", null);
         }

         FileRevision rightRevision = new FileRevision(leftSHA, leftFileName);
         string rightRevisionPath = _fileCache.GetFileRevisionPath(leftRevision);
         if (String.IsNullOrEmpty(rightRevisionPath))
         {
            throw new FullContextDiffProviderException("Cannot obtain right file revision path", null);
         }

         System.IO.File.ReadLines
      }

      private IGitCommandService _commandService;
      private FileStorageComparisonCache _comparisonCache;
      private FileStorageFileCache _fileCache;
   }
}
