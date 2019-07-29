using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrCore
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
      public CombinedContextMaker(GitRepository gitRepository)
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

         calculateNullLinesCount(startLineNumber, isRightSideContext, context,
            out int nullsAtLeft, out int nullsAtRight);
         if ((isRightSideContext && startLineNumber + nullsAtRight > context.Right.Count)
         || (!isRightSideContext && startLineNumber + nullsAtLeft > context.Left.Count))
         {
            Debug.Assert(false);
         }

         // counters of null-lines that we encounter within the loop below
         int extraNullsAtLeft = 0;
         int extraNullsAtRight = 0;

         DiffContext diffContext = new DiffContext
         {
            Lines = new List<DiffContext.Line>()
         };

         int iContextLine = 0;
         while (true)
         {
            // one-base line number within a requested context
            int ctxLineNumber = startLineNumber + iContextLine;

            // zero-based line number in 'full context diff' lists
            int absLineNumber = ctxLineNumber - 1 + (isRightSideContext ? nullsAtRight : nullsAtLeft);
            if (absLineNumber >= context.Left.Count)
            {
               // we've just reached the end
               break;
            }

            // one-based line number in sha1 file
            int leftLineNumber =
               ctxLineNumber - extraNullsAtLeft + (isRightSideContext ? nullsAtRight - nullsAtLeft : 0);

            // one-based line number in sha2 file
            int rightLineNumber =
               ctxLineNumber - extraNullsAtRight + (isRightSideContext ? 0 : nullsAtLeft - nullsAtRight);

            if ((isRightSideContext && rightLineNumber > endLineNumber)
            || (!isRightSideContext && leftLineNumber > endLineNumber))
            {
               // we've just reached a line that should not be included in the context
               break;
            }

            DiffContext.Line line =
               getLineContext(absLineNumber, leftLineNumber, rightLineNumber, context,
                  ref extraNullsAtLeft, ref extraNullsAtRight);
            diffContext.Lines.Add(line);

            if (iContextLine == 0)
            {
               // discard increments of extra null lines at the first iteration because these null lines
               // are included in nullsAtLeft and nullsAtRight
               extraNullsAtLeft = 0;
               extraNullsAtRight = 0;
            }

            if ((isRightSideContext && rightLineNumber == linenumber)
            || (!isRightSideContext && leftLineNumber == linenumber))
            {
               // zero-based index of a selected line in DiffContext.Lines
               diffContext.SelectedIndex = iContextLine;
            }

            ++iContextLine;
         }

         return diffContext;
      }

      // absLineNumber is zero-based
      // leftLineNumber and rightLineNumber are one-based
      private static DiffContext.Line getLineContext(int absLineNumber, int leftLineNumber, int rightLineNumber,
         FullContextDiff context, ref int extraNullsAtLeft, ref int extraNullsAtRight)
      {
         DiffContext.Line line = new DiffContext.Line();

         if (context.Left[absLineNumber] != null && context.Right[absLineNumber] != null)
         {
            Debug.Assert(context.Left[absLineNumber] == context.Right[absLineNumber]);
            line.Left = getSide(leftLineNumber, DiffContext.Line.State.Unchanged);
            line.Right = getSide(rightLineNumber, DiffContext.Line.State.Unchanged);
            line.Text = context.Left[absLineNumber];
         }
         else if (context.Left[absLineNumber] != null)
         {
            ++extraNullsAtRight;
            line.Left = getSide(leftLineNumber, DiffContext.Line.State.Changed);
            line.Text = context.Left[absLineNumber];
         }
         else if (context.Right[absLineNumber] != null)
         {
            ++extraNullsAtLeft;
            line.Right = getSide(rightLineNumber, DiffContext.Line.State.Changed);
            line.Text = context.Right[absLineNumber];
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

      // linenumber is one-based
      private static void calculateNullLinesCount(int linenumber, bool isRightSideContext, FullContextDiff context,
         out int nullsAtLeft, out int nullsAtRight)
      {
         int lineCount = 0; // counts lines at the right side if isRightSideContext is true
         nullsAtLeft = 0;
         nullsAtRight = 0;

         // calculate number of 'null' lines at each side
         for (int iLine = 0; iLine < context.Left.Count; ++iLine)
         {
            if (context.Left[iLine] == null)
            {
               nullsAtLeft++;
            }

            if (context.Right[iLine] == null)
            {
               nullsAtRight++;
            }

            if ((isRightSideContext && context.Right[iLine] != null)
            || (!isRightSideContext && context.Left[iLine] != null))
            {
               ++lineCount;
               if (lineCount == linenumber)
               {
                  // we're finishing to calculate 'null' lines when passed linenumber is reached
                  break;
               }
            }
         }
      }

      private readonly GitRepository _gitRepository;
   }
}

