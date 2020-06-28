using System;
using mrHelper.Common.Exceptions;

namespace mrHelper.StorageSupport
{
   public class FileRenameDetectorException : ExceptionEx
   {
      internal FileRenameDetectorException(string message, Exception innerException)
         : base(message, innerException) { }
   }

   public interface IFileRenameDetector
   {
      string IsRenamed(string leftcommit, string rightcommit, string filename, bool leftsidename, out bool moved);
   }
}

