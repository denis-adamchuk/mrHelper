using System;
using System.Diagnostics;
using mrHelper.Core.Context;
using mrHelper.Common.Tools;
using mrHelper.StorageSupport;

namespace mrHelper.Core.Matching
{
   /// <summary>
   /// Matches SHA of two commits to a line position in two files.
   /// See matching rules in comments for DiffPosition structure.
   /// Cost: one 'git diff -U20000' for each Match() call.
   /// </summary>
   public class LineNumberMatcher
   {
      public LineNumberMatcher(IGitCommandService git)
      {
         _git = git;
      }

      /// <summary>
      /// Return DiffPosition if match succeeded and throw if match failed
      /// Throws ArgumentException in case of bad arguments.
      /// Throws MatchingException.
      /// </summary>
      public void Match(DiffRefs refs, string leftPath, string rightPath,
         int lineNumber, bool isLeftSideLine, out string leftLineNumber, out string rightLineNumber)
      {
         int? oppositeLine;
         try
         {
            oppositeLine = getOppositeLine(refs, isLeftSideLine, leftPath, rightPath, lineNumber);
         }
         catch (BadPosition)
         {
            throw new ArgumentException(
               String.Format("Bad position: Line Number {0} ({1})", lineNumber, isLeftSideLine ? "Left" : "Right"));
         }
         catch (FullContextDiffProviderException ex)
         {
            throw new MatchingException("Cannot match lines", ex);
         }

         string currentLineAsString = lineNumber.ToString();
         string oppositeLineAsString = oppositeLine?.ToString();
         leftLineNumber = isLeftSideLine ? currentLineAsString : oppositeLineAsString;
         rightLineNumber = isLeftSideLine ? oppositeLineAsString : currentLineAsString;
      }

      private int? getOppositeLine(DiffRefs refs, bool isLeftSide, string leftFileName, string rightFileName,
         int lineNumber)
      {
         FullContextDiff context = _git.FullContextDiffProvider.GetFullContextDiff(
            refs.LeftSHA, refs.RightSHA, leftFileName, rightFileName);

         Debug.Assert(context.Left.Count == context.Right.Count);

         SparsedList<string> currentList = isLeftSide ? context.Left : context.Right;
         SparsedList<string> oppositeList = isLeftSide ? context.Right : context.Left;

         SparsedListIterator<string> itCurrentList = SparsedListUtils.FindNth(currentList.Begin(), lineNumber - 1);
         SparsedListIterator<string> itOppositeList = SparsedListUtils.Advance(oppositeList.Begin(), itCurrentList.Position);

         return itOppositeList.GetLineNumber() == null ? new int?() : itOppositeList.GetLineNumber().Value + 1;
      }

      private readonly IGitCommandService _git;
   }
}

