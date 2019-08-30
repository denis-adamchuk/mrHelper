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
   /// Matches SHA of two commits to the line/side details obtained from diff tool.
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
      public DiffPosition Match(DiffRefs diffRefs, DiffToolInfo difftoolInfo)
      {
         if (!difftoolInfo.IsValid())
         {
            throw new ArgumentException(
               String.Format("Bad diff tool info: {0}", difftoolInfo.ToString()));
         }

         MatchResult matchResult = match(diffRefs, difftoolInfo);
         return createPosition(matchResult, diffRefs, difftoolInfo);
      }

      private DiffPosition createPosition(MatchResult matchResult, DiffRefs diffRefs, DiffToolInfo difftoolInfo)
      {
         string leftPath = String.Empty;
         string rightPath = String.Empty;
         getPositionPaths(ref difftoolInfo, ref leftPath, ref rightPath);

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

      private void getPositionPaths(ref DiffToolInfo difftoolInfo, ref string LeftPath, ref string RightPath)
      {
         if (difftoolInfo.Left.HasValue && !difftoolInfo.Right.HasValue)
         {
            // When a file is removed, diff tool does not provide a right-side file name
            LeftPath = difftoolInfo.Left.Value.FileName;
            RightPath = LeftPath;
         }
         else if (!difftoolInfo.Left.HasValue && difftoolInfo.Right.HasValue)
         {
            // When a file is added, diff tool does not provide a left-side file name
            LeftPath = difftoolInfo.Right.Value.FileName;
            RightPath = LeftPath;
         }
         else if (difftoolInfo.Left.HasValue && difftoolInfo.Right.HasValue)
         {
            // Filenames may be different and may be the same, it does not matter here
            LeftPath = difftoolInfo.Left.Value.FileName;
            RightPath = difftoolInfo.Right.Value.FileName;
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

      private MatchResult match(DiffRefs diffRefs, DiffToolInfo difftoolInfo)
      {
         FullContextDiffProvider provider = new FullContextDiffProvider(_gitRepository);
         FullContextDiff context =
            provider.GetFullContextDiff(diffRefs.LeftSHA, diffRefs.RightSHA,
               difftoolInfo.Left?.FileName ?? null, difftoolInfo.Right?.FileName ?? null);

         Debug.Assert(context.Left.Count == context.Right.Count);

         bool isCurrentSideLeft = difftoolInfo.IsLeftSideCurrent;
         DiffToolInfo.Side currentSide = isCurrentSideLeft ? difftoolInfo.Left.Value : difftoolInfo.Right.Value;

         SparsedList<string> firstList = isCurrentSideLeft ? context.Left : context.Right;
         SparsedList<string> secondList = isCurrentSideLeft ? context.Right : context.Left;

         SparsedListIterator<string> itFirst = SparsedListUtils.FindNth(firstList.Begin(), currentSide.LineNumber - 1);
         SparsedListIterator<string> itSecond = SparsedListUtils.Advance(secondList.Begin(), itFirst.Position);

         return new MatchResult
         {
            LeftLineNumber = isCurrentSideLeft ? currentSide.LineNumber : itSecond.LineNumber + 1,
            RightLineNumber = isCurrentSideLeft ? itSecond.LineNumber + 1 : currentSide.LineNumber
         };
      }

      private readonly IGitRepository _gitRepository;
   }
}

