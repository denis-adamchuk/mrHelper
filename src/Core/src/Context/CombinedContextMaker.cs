using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mrHelper.Core.Matching;
using mrHelper.Core.Interprocess;
using mrHelper.Common.Interfaces;

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
      public CombinedContextMaker(IGitRepository gitRepository)
      {
         Debug.Assert(gitRepository != null);
         _gitRepository = gitRepository;
      }

      /// <summary>
      /// Throws ArgumentException.
      /// Throws GitOperationException in case of problems with git.
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

         TwoListIterator<string> iterator =
            new TwoListIterator<string>(context.Left, context.Right, startLineNumber - 1, isRightSideContext);

         DiffContext diffContext = new DiffContext
         {
            Lines = new List<DiffContext.Line>()
         };

         int iContextLine = 0;
         while (true)
         {
            int? leftLineNumber = iterator.LeftLineNumber() != null ? iterator.LeftLineNumber() + 1 : null;
            int? rightLineNumber = iterator.RightLineNumber() != null ? iterator.RightLineNumber() + 1 : null;

            DiffContext.Line line =
               getLineContext(leftLineNumber, rightLineNumber, iterator.LeftLine(), iterator.RightLine());
            diffContext.Lines.Add(line);

            if ((leftLineNumber.HasValue && !isRightSideContext && leftLineNumber == linenumber)
            || (rightLineNumber.HasValue && isRightSideContext && rightLineNumber == linenumber))
            {
               // zero-based index of a selected line in DiffContext.Lines
               diffContext.SelectedIndex = iContextLine;
            }

            if ((leftLineNumber.HasValue && !isRightSideContext && leftLineNumber >= endLineNumber)
            || (rightLineNumber.HasValue && isRightSideContext && rightLineNumber >= endLineNumber))
            {
               // we've just reached a line that should not be included in the context
               break;
            }

            if (!iterator.Next())
            {
               // we've just reached the end
               break;
            }

            ++iContextLine;
         }

         return diffContext;
      }

      // leftLineNumber and rightLineNumber are one-based
      private static DiffContext.Line getLineContext(int? leftLineNumber, int? rightLineNumber,
         string leftLine, string rightLine)
      {
         DiffContext.Line line = new DiffContext.Line();

         if (leftLineNumber.HasValue && rightLineNumber.HasValue)
         {
            Debug.Assert(leftLine == rightLine);
            line.Left = getSide(leftLineNumber.Value, DiffContext.Line.State.Unchanged);
            line.Right = getSide(rightLineNumber.Value, DiffContext.Line.State.Unchanged);
            line.Text = leftLine;
         }
         else if (leftLineNumber.HasValue)
         {
            line.Left = getSide(leftLineNumber.Value, DiffContext.Line.State.Changed);
            line.Text = leftLine;
         }
         else if (rightLineNumber.HasValue)
         {
            line.Right = getSide(rightLineNumber.Value, DiffContext.Line.State.Changed);
            line.Text = rightLine;
         }
         else
         {
            Debug.Assert(false);
         }

         return line;
      }

      private static DiffContext.Line.Side getSide(int lineNumber, DiffContext.Line.State state)
      {
         DiffContext.Line.Side side = new DiffContext.Line.Side
         {
            Number = lineNumber,
            State = state
         };
         return side;
      }

      private readonly IGitRepository _gitRepository;
   }
}

