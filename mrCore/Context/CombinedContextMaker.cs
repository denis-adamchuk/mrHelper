using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrCore
{
   // This 'maker' creates a list of lines that belong to both left and right sides.
   // List starts from the line passed to GetContext() and includes a context with adjacent
   // removed, added and unmodified.
   //
   // Cost: one 'git diff -U20000' call for each GetContext() call.
   public class CombinedContextMaker : ContextMaker
   {
      public CombinedContextMaker(GitRepository gitRepository)
      {
         _gitRepository = gitRepository;
      }

      public DiffContext GetContext(Position position, int size)
      {
         // TODO Is it ok that we cannot handle different filenames?
         Debug.Assert(position.NewPath == position.OldPath);
         Debug.Assert(position.NewLine != null || position.OldLine != null);

         // If NewLine is valid, then it points to either added/modified or unchanged line, handle them the same way
         bool isRightSideContext = position.NewLine != null;
         int linenumber = isRightSideContext ? int.Parse(position.NewLine) : int.Parse(position.OldLine);
         string filename = isRightSideContext ? position.NewPath : position.OldPath;
         string leftSHA = position.Refs.BaseSHA;
         string rightSHA = position.Refs.HeadSHA;

         FullContextDiffProvider provider = new FullContextDiffProvider(_gitRepository);
         FullContextDiff context = provider.GetFullContextDiff(leftSHA, rightSHA, filename);
         Debug.Assert(context.Left.Count == context.Right.Count);

         return createDiffContext(linenumber, isRightSideContext, context, size);
      }

      // isRightSideContext is true when linenumber corresponds to the right side (sha2)
      private DiffContext createDiffContext(int linenumber, bool isRightSideContext, FullContextDiff context, int size)
      {
         int nullsAtLeft = 0;
         int nullsAtRight = 0;
         calculateNullLinesCount(linenumber, isRightSideContext, context, out nullsAtLeft, out nullsAtRight);
         if (linenumber <= 0
            || (isRightSideContext && linenumber + nullsAtRight > context.Right.Count)
            || (!isRightSideContext && linenumber + nullsAtLeft > context.Left.Count))
         {
            Debug.Assert(false);
            return new DiffContext();
         }

         // counters of null-lines that we encounter within the loop below
         int extraNullsAtLeft = 0;
         int extraNullsAtRight = 0;

         DiffContext diffContext = new DiffContext();
         diffContext.Lines = new List<DiffContext.Line>();
         for (int iContextLine = 0; iContextLine < size; ++iContextLine)
         {
            // one-base line number within a requested context
            int ctxLineNumber = linenumber + iContextLine;

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
         }
         return diffContext;
      }

      private static DiffContext.Line getLineContext(int absLineNumber, int leftLineNumber, int rightLineNumber,
         FullContextDiff context, ref int extraNullsAtLeft, ref int extraNullsAtRight)
      {
         DiffContext.Line line = new DiffContext.Line();

         if (context.Left[absLineNumber] != null & context.Right[absLineNumber] != null)
         {
            Debug.Assert(context.Left[absLineNumber] == context.Right[absLineNumber]);
            line.Left = getSide(leftLineNumber, false, DiffContext.Line.State.Unchanged);
            line.Right = getSide(rightLineNumber, true, DiffContext.Line.State.Unchanged);
            line.Text = context.Left[absLineNumber];
         }
         else if (context.Left[absLineNumber] != null)
         {
            ++extraNullsAtRight;
            line.Left = getSide(leftLineNumber, false, DiffContext.Line.State.Removed);
            line.Text = context.Left[absLineNumber];
         }
         else if (context.Right[absLineNumber] != null)
         {
            ++extraNullsAtLeft;
            line.Right = getSide(rightLineNumber, true, DiffContext.Line.State.Added);
            line.Text = context.Right[absLineNumber];
         }
         else
         {
            Debug.Assert(false);
         }

         return line;
      }

      private static DiffContext.Line.Side getSide(int lineNumber, bool right, DiffContext.Line.State state)
      {
         DiffContext.Line.Side side = new DiffContext.Line.Side();
         side.Number = lineNumber;
         side.State = state;
         return side;
      }

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

      GitRepository _gitRepository;
   }
}

