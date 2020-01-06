﻿using System;
using System.Collections.Generic;
using mrHelper.Common.Tools;
using mrHelper.Common.Constants;
using mrHelper.Common.Interfaces;
using System.Diagnostics;

namespace mrHelper.Core.Context
{
   /// <summary>
   /// Contains a diff between two revisions with all lines from each of revision including missing lines.
   /// </summary>
   public struct FullContextDiff
   {
      public SparsedList<string> Left;
      public SparsedList<string> Right;
   }

   /// <summary>
   /// Provides two lists of the same size. First list contains lines from sha1 and null for missing lines.
   /// Seconds list contains lines from sha2 and null for missing lines.
   /// </summary>
   public class FullContextDiffProvider
   {
      public FullContextDiffProvider(IGitRepository gitRepository)
      {
         _gitRepository = gitRepository;
      }

      /// <summary>
      /// Throws GitOperationException in case of problems with git.
      /// </summary>
      public FullContextDiff GetFullContextDiff(string leftSHA, string rightSHA,
         string leftFileName, string rightFileName)
      {
         FullContextDiff fullContextDiff = new FullContextDiff
         {
            Left = new SparsedList<string>(),
            Right = new SparsedList<string>()
         };

         GitDiffArguments arguments = new GitDiffArguments
         {
            sha1 = leftSHA,
            sha2 = rightSHA,
            filename1 = leftFileName,
            filename2 = rightFileName,
            context = Constants.FullContextSize
         };

         List<string> fullDiff = _gitRepository.Diff(arguments);
         if (fullDiff.Count == 0)
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
            var lineOrig = line.Substring(1, line.Length - 1);
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
         return fullContextDiff;
      }

      private readonly IGitRepository _gitRepository;
   }
}

