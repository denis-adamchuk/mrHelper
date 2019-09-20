using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using mrHelper.Core.Git;
using mrHelper.Core.Tools;
using mrHelper.Core.Context;
using mrHelper.Core.Interprocess;
using mrHelper.Common.Interfaces;

namespace mrHelper.Core.Matching
{
   /// <summary>
   /// Matches SHA of two commits to a line position in two files.
   /// See matching rules in comments for DiffPosition structure.
   /// Cost: one 'git diff -U20000' for each Match() call.
   /// </summary>
   public class RefToLineMatcher
   {
      // Internal matching state
      private struct MatchResult
      {
         public int? LeftLineNumber;
         public int? RightLineNumber;
      }

      public RefToLineMatcher(IGitRepository gitRepository)
      {
         _gitRepository = gitRepository;
      }

      /// <summary>
      /// Return DiffPosition if match succeeded and throw if match failed
      /// Throws ArgumentException in case of bad arguments.
      /// Throws GitOperationException in case of problems with git.
      /// </summary>
      public DiffPosition Match(DiffRefs diffRefs, LineMatchInfo lineMatchInfo)
      {
         if (!lineMatchInfo.IsValid())
         {
            throw new ArgumentException(
               String.Format("Bad line match info: {0}", lineMatchInfo.ToString()));
         }

         MatchResult matchResult = match(diffRefs, lineMatchInfo);
         return createPosition(matchResult, diffRefs, lineMatchInfo);
      }

      private DiffPosition createPosition(MatchResult matchResult, DiffRefs diffRefs, LineMatchInfo lineMatchInfo)
      {
         string leftPath = String.Empty;
         string rightPath = String.Empty;
         getPositionPaths(ref lineMatchInfo, ref leftPath, ref rightPath);

         string leftLine = String.Empty;
         string rightLine = String.Empty;
         getPositionLines(matchResult, ref leftLine, ref rightLine);

         DiffPosition position = new DiffPosition
         {
            Refs = diffRefs,
            LeftPath = leftPath,
            LeftLine = leftLine,
            RightPath = rightPath,
            RightLine = rightLine
         };
         return position;
      }

      private void getPositionPaths(ref LineMatchInfo lineMatchInfo, ref string LeftPath, ref string RightPath)
      {
         if (lineMatchInfo.LeftFileName != String.Empty && lineMatchInfo.RightFileName == String.Empty)
         {
            // When a file is removed, right-side file name is missing
            LeftPath = lineMatchInfo.LeftFileName;
            RightPath = LeftPath;
         }
         else if (lineMatchInfo.LeftFileName == String.Empty && lineMatchInfo.RightFileName != String.Empty)
         {
            // When a file is added, left-side file name is missing
            LeftPath = lineMatchInfo.RightFileName;
            RightPath = LeftPath;
         }
         else if (lineMatchInfo.LeftFileName != String.Empty && lineMatchInfo.RightFileName != String.Empty)
         {
            // Filenames may be different and may be the same, it does not matter here
            LeftPath = lineMatchInfo.LeftFileName;
            RightPath = lineMatchInfo.RightFileName;
         }
         else
         {
            Debug.Assert(false);
         }
      }

      private void getPositionLines(MatchResult matchResult, ref string leftLine, ref string rightLine)
      {
         leftLine = matchResult.LeftLineNumber.HasValue ? matchResult.LeftLineNumber.Value.ToString() : null;
         rightLine = matchResult.RightLineNumber.HasValue ? matchResult.RightLineNumber.Value.ToString() : null;
      }

      private MatchResult match(DiffRefs diffRefs, LineMatchInfo lineMatchInfo)
      {
         FullContextDiffProvider provider = new FullContextDiffProvider(_gitRepository);
         FullContextDiff context =
            provider.GetFullContextDiff(diffRefs.LeftSHA, diffRefs.RightSHA,
               lineMatchInfo.LeftFileName, lineMatchInfo.RightFileName);

         Debug.Assert(context.Left.Count == context.Right.Count);

         bool isCurrentSideLeft = lineMatchInfo.IsLeftSideLineNumber;

         SparsedList<string> firstList = isCurrentSideLeft ? context.Left : context.Right;
         SparsedList<string> secondList = isCurrentSideLeft ? context.Right : context.Left;

         SparsedListIterator<string> itFirst = SparsedListUtils.FindNth(firstList.Begin(), lineMatchInfo.LineNumber - 1);
         SparsedListIterator<string> itSecond = SparsedListUtils.Advance(secondList.Begin(), itFirst.Position);

         return new MatchResult
         {
            LeftLineNumber = isCurrentSideLeft ? lineMatchInfo.LineNumber : itSecond.LineNumber + 1,
            RightLineNumber = isCurrentSideLeft ? itSecond.LineNumber + 1 : lineMatchInfo.LineNumber
         };
      }

      private readonly IGitRepository _gitRepository;
   }
}

