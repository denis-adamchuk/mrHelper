using System;
using System.Collections.Generic;
using System.Diagnostics;
using mrHelper.Core.Matching;
using mrHelper.Common.Tools;
using mrHelper.StorageSupport;

namespace mrHelper.Core.Context
{
   /// <summary>
   /// This 'maker' creates a list of lines that belong to both left and right sides.
   /// List starts from the line passed to GetContext() and includes a context with adjacent
   /// removed, added and unmodified.
   ///
   /// Cost: one 'git diff -U20000' call for each GetContext() call.
   /// </summary>
   public class CombinedContextMaker : IContextMaker
   {
      public CombinedContextMaker(IGitCommitStorage gitRepository)
      {
         Debug.Assert(gitRepository != null);
         _gitRepository = gitRepository;
      }

      /// <summary>
      /// Throws ArgumentException, ContextMakingException.
      /// </summary>
      public DiffContext GetContext(DiffPosition position, ContextDepth depth)
      {
         if (!Context.Helpers.IsValidPosition(position))
         {
            throw new ArgumentException(
               String.Format("Bad \"position\": {0}", position.ToString()));
         }

         if (!Context.Helpers.IsValidContextDepth(depth))
         {
            throw new ArgumentException(
               String.Format("Bad \"depth\": {0}", depth.ToString()));
         }

         // If RightLine is valid, then it points to either added/modified or unchanged line, handle them the same way
         bool isRightSideContext = position.RightLine != null;
         int linenumber = isRightSideContext ? int.Parse(position.RightLine) : int.Parse(position.LeftLine);
         string leftFilename = position.LeftPath;
         string rightFilename = position.RightPath;
         string leftSHA = position.Refs.LeftSHA;
         string rightSHA = position.Refs.RightSHA;

         FullContextDiffProvider provider = new FullContextDiffProvider(_gitRepository);
         FullContextDiff context = provider.GetFullContextDiff(leftSHA, rightSHA, leftFilename, rightFilename);
         Debug.Assert(context.Left.Count == context.Right.Count);
         if (linenumber > context.Left.Count)
         {
            throw new ArgumentException(
               String.Format("Line number {0} is greater than total line number count, invalid \"position\": {1}",
               linenumber.ToString(), position.ToString()));
         }

         return createDiffContext(linenumber, isRightSideContext, context, depth);
      }

      // isRightSideContext is true when linenumber corresponds to the right side (sha2).
      // linenumber is one-based
      private DiffContext createDiffContext(int linenumber, bool isRightSideContext, FullContextDiff context,
         ContextDepth depth)
      {
         int startLineNumber = Math.Max(1, linenumber - depth.Up);
         int endLineNumber = linenumber + depth.Down;

         SparsedListIterator<string> itLeft = context.Left.Begin();
         SparsedListIterator<string> itRight = context.Right.Begin();
         if (isRightSideContext)
         {
            itRight = SparsedListUtils.FindNth(itRight, startLineNumber - 1);
            itLeft = SparsedListUtils.Advance(itLeft, itRight.Position);
         }
         else
         {
            itLeft = SparsedListUtils.FindNth(itLeft, startLineNumber - 1);
            itRight = SparsedListUtils.Advance(itRight, itLeft.Position);
         }

         int iContextLine = 0;
         int selectedIndex = 0;
         List<DiffContext.Line> lines = new List<DiffContext.Line>();

         while (true)
         {
            int? leftLineNumber = itLeft.GetLineNumber() != null ? itLeft.GetLineNumber() + 1 : null;
            int? rightLineNumber = itRight.GetLineNumber() != null ? itRight.GetLineNumber() + 1 : null;

            DiffContext.Line line = getLineContext(leftLineNumber, rightLineNumber, itLeft.GetCurrent(), itRight.GetCurrent());
            lines.Add(line);

            if ((leftLineNumber.HasValue && !isRightSideContext && leftLineNumber == linenumber)
            || (rightLineNumber.HasValue && isRightSideContext && rightLineNumber == linenumber))
            {
               // zero-based index of a selected line in DiffContext.Lines
               selectedIndex = iContextLine;
            }

            if ((leftLineNumber.HasValue && !isRightSideContext && leftLineNumber >= endLineNumber)
            || (rightLineNumber.HasValue && isRightSideContext && rightLineNumber >= endLineNumber))
            {
               // we've just reached a line that should not be included in the context
               break;
            }

            if (!itLeft.Next() || !itRight.Next())
            {
               // we've just reached the end
               break;
            }

            ++iContextLine;
         }

         return new DiffContext(lines, selectedIndex);
      }

      // leftLineNumber and rightLineNumber are one-based
      private static DiffContext.Line getLineContext(int? leftLineNumber, int? rightLineNumber,
         string leftLine, string rightLine)
      {
         DiffContext.Line.Side? leftSide = null;
         DiffContext.Line.Side? rightSide = null;
         string text = String.Empty;

         if (leftLineNumber.HasValue && rightLineNumber.HasValue)
         {
            Debug.Assert(leftLine == rightLine);
            leftSide = new DiffContext.Line.Side(leftLineNumber.Value, DiffContext.Line.State.Unchanged);
            rightSide = new DiffContext.Line.Side(rightLineNumber.Value, DiffContext.Line.State.Unchanged);
            text = leftLine;
         }
         else if (leftLineNumber.HasValue)
         {
            leftSide = new DiffContext.Line.Side(leftLineNumber.Value, DiffContext.Line.State.Changed);
            text = leftLine;
         }
         else if (rightLineNumber.HasValue)
         {
            rightSide = new DiffContext.Line.Side(rightLineNumber.Value, DiffContext.Line.State.Changed);
            text = rightLine;
         }
         else
         {
            Debug.Assert(false);
         }

         return new DiffContext.Line(text, leftSide, rightSide);
      }

      private readonly IGitCommitStorage _gitRepository;
   }
}

