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
   public class LineNumberMatcher
   {
      public LineNumberMatcher(IGitRepository gitRepository)
      {
         _gitRepository = gitRepository;
      }

      /// <summary>
      /// Return DiffPosition if match succeeded and throw if match failed
      /// Throws ArgumentException in case of bad arguments.
      /// Throws GitOperationException in case of problems with git.
      /// </summary>
      public bool Match(MatchInfo matchInfo, DiffPosition inDiffPosition, out DiffPosition outDiffPosition)
      {
         if (!matchInfo.IsValid())
         {
            throw new ArgumentException(
               String.Format("Bad match info: {0}", matchInfo.ToString()));
         }

         DiffRefs refs = inDiffPosition.Refs;
         int oppositeLine = getOppositeLine(refs, matchInfo);

         int currentLine = matchInfo.LineNumber;
         bool isLeftSide = matchInfo.IsLeftSideLineNumber;

         outDiffPosition = inDiffPosition;
         outDiffPosition.LeftLine = (isLeftSide ? currentLine : oppositeLine).ToString();
         outDiffPosition.RightLine = (isLeftSide ? oppositeLine : currentLine).ToString();
         return true;
      }

      private int getOppositeLine(DiffRefs refs, MatchInfo matchInfo)
      {
         FullContextDiffProvider provider = new FullContextDiffProvider(_gitRepository);
         FullContextDiff context = provider.GetFullContextDiff(
            refs.LeftSHA, refs.RightSHA, matchInfo.LeftFileName, matchInfo.RightFileName);

         Debug.Assert(context.Left.Count == context.Right.Count);

         SparsedList<string> currentList = matchInfo.IsLeftSideLineNumber ? context.Left : context.Right;
         SparsedList<string> oppositeList = matchInfo.IsLeftSideLineNumber ? context.Right : context.Left;

         SparsedListIterator<string> itCurrentList = SparsedListUtils.FindNth(currentList.Begin(), matchInfo.LineNumber - 1);
         SparsedListIterator<string> itOppositeList = SparsedListUtils.Advance(oppositeList.Begin(), itCurrentList.Position);

         // TODO How it worked before w/o Value?
         return itOppositeList.LineNumber.Value + 1;
      }

      private readonly IGitRepository _gitRepository;
   }
}

