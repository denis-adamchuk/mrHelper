using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using mrHelper.Common.Tools;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;

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
      public FullContextDiffProviderException(string message, Exception innerException)
         : base(message, innerException)
      {
      }
   }


   /// <summary>
   /// Provides two lists of the same size. First list contains lines from sha1 and null for missing lines.
   /// Seconds list contains lines from sha2 and null for missing lines.
   /// </summary>
   public class FullContextDiffProvider
   {
      public FullContextDiffProvider(IGitCommandService git)
      {
         _git = git;
      }

      /// <summary>
      /// Throws FullContextDiffProviderException.
      /// </summary>
      public FullContextDiff GetFullContextDiff(string leftSHA, string rightSHA,
         string leftFileName, string rightFileName)
      {
         CacheKey key = new CacheKey(leftSHA, rightSHA, leftFileName, rightFileName);
         if (_cachedContexts.TryGetValue(key, out FullContextDiff context))
         {
            return context;
         }

         FullContextDiff fullContextDiff = new FullContextDiff(new SparsedList<string>(), new SparsedList<string>());

         GitDiffArguments arguments = new GitDiffArguments(
            GitDiffArguments.DiffMode.Context,
            new GitDiffArguments.CommonArguments(leftSHA, rightSHA, leftFileName, rightFileName, null),
            new GitDiffArguments.DiffContextArguments(Constants.FullContextSize));

         IEnumerable<string> fullDiff;
         try
         {
            fullDiff = _git?.ShowDiff(arguments);
         }
         catch (GitNotAvailableDataException ex)
         {
            throw new FullContextDiffProviderException("Cannot obtain git diff", ex);
         }

         if (fullDiff == null)
         {
            throw new FullContextDiffProviderException("Cannot obtain git diff", null);
         }

         if (fullDiff.Count() == 0)
         {
            Trace.TraceWarning(String.Format(
               "[FullContextDiffProvider] Context size is zero. LeftSHA: {0}, Right SHA: {1}, Left file: {2}, Right file: {3}",
               leftSHA, rightSHA, leftFileName, rightFileName));
         }

         bool skip = true;
         foreach (string line in fullDiff)
         {
            char sign = line[0];
            if (skip)
            {
               // skip meta information about diff
               if (sign == '@')
               {
                  // next lines should not be skipped because they contain a diff itself
                  skip = false;
               }
               continue;
            }
            string lineOrig = line.Substring(1, line.Length - 1);
            switch (sign)
            {
               case '-':
                  fullContextDiff.Left.Add(lineOrig);
                  fullContextDiff.Right.Add(null);
                  break;
               case '+':
                  fullContextDiff.Left.Add(null);
                  fullContextDiff.Right.Add(lineOrig);
                  break;
               case ' ':
                  fullContextDiff.Left.Add(lineOrig);
                  fullContextDiff.Right.Add(lineOrig);
                  break;
            }
         }

         _cachedContexts.Add(key, fullContextDiff);
         return fullContextDiff;
      }

      private readonly IGitCommandService _git;

      private struct CacheKey
      {
         public CacheKey(string leftSha, string rightSha, string leftPath, string rightPath)
         {
            LeftSha = leftSha;
            RightSha = rightSha;
            LeftPath = leftPath;
            RightPath = rightPath;
         }

         internal string LeftSha { get; }
         internal string RightSha { get; }
         internal string LeftPath { get; }
         internal string RightPath { get; }
      }

      private readonly SelfCleanUpDictionary<CacheKey, FullContextDiff> _cachedContexts =
         new SelfCleanUpDictionary<CacheKey, FullContextDiff>(CacheCleanupPeriodSeconds);

      private readonly static int CacheCleanupPeriodSeconds = 60 * 60 * 24 * 5; // 5 days
   }
}

