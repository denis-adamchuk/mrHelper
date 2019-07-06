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
         string sha1 = position.Refs.BaseSHA;
         string sha2 = position.Refs.HeadSHA;

         FullContextDiffProvider provider = new FullContextDiffProvider(_gitRepository);
         return createDiffContext(linenumber, isRightSideContext, provider.GetFullContextDiff(sha1, sha2, filename), size);
      }

      // isRightSideContext is true when linenumber corresponds to the right side (sha2)
      private DiffContext createDiffContext(int linenumber, bool isRightSideContext, FullContextDiff context, int size)
      {
         Debug.Assert(linenumber <= context.sha1context.Count);
         Debug.Assert(context.sha1context.Count == context.sha2context.Count);

         int nullsAtLeft = 0;
         int nullsAtRight = 0;
         calculateNullLinesCount(linenumber, isRightSideContext, context, out nullsAtLeft, out nullsAtRight);

         // counters of null-lines that we encounter within the loop below
         int sha1Extra = 0;
         int sha2Extra = 0;

         DiffContext diffContext = new DiffContext();
         diffContext.Lines = new List<DiffContext.Line>();
         for (int iContextLine = 0; iContextLine < size; ++iContextLine)
         {
            int ctxLineNumber = linenumber + iContextLine;

            // zero-based line number in 'full context diff' lists
            int absLineNumber = ctxLineNumber - 1 + (isRightSideContext ? nullsAtRight : nullsAtLeft);

            // one-based line number in sha1 file
            int sha1LineNumber = ctxLineNumber - sha1Extra + (isRightSideContext ? nullsAtRight - nullsAtLeft : 0);

            // one-based line number in sha2 file
            int sha2LineNumber = ctxLineNumber - sha2Extra + (isRightSideContext ? 0 : nullsAtLeft - nullsAtRight);

            diffContext.Lines.Add(getLineContext(absLineNumber, sha1LineNumber, sha2LineNumber, context,
               ref sha1Extra, ref sha2Extra));

            if (iContextLine == 0)
            {
               // discard increments of extra null lines at the first iteration because these null lines
               // are included in nullsAtLeft and nullsAtRight
               sha1Extra = 0;
               sha2Extra = 0;
            }
         }
         return diffContext;
      }

      private static DiffContext.Line getLineContext(int absLineNumber, int sha1LineNumber, int sha2LineNumber,
         FullContextDiff context, ref int sha1Extra, ref int sha2Extra)
      {
         DiffContext.Line line = new DiffContext.Line();
         line.Sides = new List<DiffContext.Line.Side>();

         if (context.sha1context[absLineNumber] != null & context.sha2context[absLineNumber] != null)
         {
            line.Sides.Add(getSide(sha1LineNumber, false, DiffContext.Line.State.Unchanged));
            line.Sides.Add(getSide(sha2LineNumber, true, DiffContext.Line.State.Unchanged));
         }
         else if (context.sha1context[absLineNumber] != null)
         {
            ++sha2Extra;
            line.Sides.Add(getSide(sha1LineNumber, false, DiffContext.Line.State.Removed));
         }
         else if (context.sha2context[absLineNumber] != null)
         {
            ++sha1Extra;
            line.Sides.Add(getSide(sha2LineNumber, true, DiffContext.Line.State.Added));
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
         side.State = state;
         side.Number = lineNumber;
         side.Right = right;
         return side;
      }

      private static void calculateNullLinesCount(int linenumber, bool isRightSideContext, FullContextDiff context,
         out int nullsAtLeft, out int nullsAtRight)
      {
         int lineCount = 0; // counts lines at the right side if isRightSideContext is true
         nullsAtLeft = 0;
         nullsAtRight = 0;

         // calculate number of 'null' lines at each side
         for (int iLine = 0; iLine < context.sha1context.Count; ++iLine)
         {
            if (context.sha1context[iLine] == null)
            {
               nullsAtLeft++;
            }

            if (context.sha2context[iLine] == null)
            {
               nullsAtRight++;
            }

            if ((isRightSideContext && context.sha2context[iLine] != null)
            || (!isRightSideContext && context.sha1context[iLine] != null))
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

