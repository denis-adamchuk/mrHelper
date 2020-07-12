using System;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Tools;

namespace mrHelper.StorageSupport
{
   /// <summary>
   /// Contains a diff between two revisions with all lines from each of revision including missing lines.
   /// </summary>
   public struct FullContextDiff
   {
      public FullContextDiff(SparsedList<string> left, SparsedList<string> right)
      {
         Left = left;
         Right = right;
      }

      public SparsedList<string> Left { get; }
      public SparsedList<string> Right { get; }
   }

   public class FullContextDiffProviderException : ExceptionEx
   {
      internal FullContextDiffProviderException(string message, Exception innerException)
         : base(message, innerException) { }
   }

   /// <summary>
   /// Provides two lists of the same size. First list contains lines from sha1 and null for missing lines.
   /// Seconds list contains lines from sha2 and null for missing lines.
   /// </summary>
   interface IFullContextDiffProvider
   {
      FullContextDiff GetFullContextDiff(string leftSHA, string rightSHA,
         string leftFileName, string rightFileName);
   }
}

