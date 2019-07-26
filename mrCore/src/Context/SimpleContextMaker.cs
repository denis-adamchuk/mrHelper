﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace mrCore
{
   // This 'maker' creates a list of lines for either left or right side.
   // The list starts from a line that is passed to GetContext() method and contains 'size' lines from the same side as
   // the first line. If the first line is 'unmodified' then resulting list contains 'size' lines from the right side.
   //
   // Cost: one 'git show' command for each GetContext() call.
   public class SimpleContextMaker : IContextMaker
   {
      public SimpleContextMaker(GitRepository gitRepository)
      {
         Debug.Assert(gitRepository != null);
         _gitRepository = gitRepository;
      }

      public DiffContext GetContext(DiffPosition position, ContextDepth depth)
      {
         if (!Context.Helpers.IsValidPosition(position) || !Context.Helpers.IsValidContextDepth(depth))
         {
            return new DiffContext();
         }

         bool isRightSideContext = position.RightLine != null;
         int linenumber = isRightSideContext ? int.Parse(position.RightLine) : int.Parse(position.LeftLine);
         string filename = isRightSideContext ? position.RightPath : position.LeftPath;
         string sha = isRightSideContext ? position.Refs.RightSHA : position.Refs.LeftSHA;

         return createDiffContext(linenumber, filename, sha, isRightSideContext, depth);
      }

      // isRightSideContext is true when linenumber and sha correspond to the right side
      // linenumber is one-based
      private DiffContext createDiffContext(int linenumber, string filename, string sha, bool isRightSideContext,
         ContextDepth depth)
      {
         DiffContext diffContext = new DiffContext
         {
            Lines = new List<DiffContext.Line>()
         };

         List<string> contents = _gitRepository.ShowFileByRevision(filename, sha);
         if (linenumber > contents.Count)
         {
            return new DiffContext();
         }

         int startLineNumber = Math.Max(1, linenumber - depth.Up);
         IEnumerable<string> shiftedContents = contents.Skip(startLineNumber - 1);
         foreach (string text in shiftedContents)
         {
            diffContext.Lines.Add(getContextLine(startLineNumber + diffContext.Lines.Count, isRightSideContext, text));
            if (diffContext.Lines.Count == depth.Size + 1)
            {
               break;
            }
         }

         // zero-based index of a selected line in DiffContext.Lines
         diffContext.SelectedIndex = linenumber - startLineNumber;
         return diffContext;
      }

      // linenumber is one-based
      private static DiffContext.Line getContextLine(int linenumber, bool isRightSideContext, string text)
      {
         DiffContext.Line line = new DiffContext.Line
         {
            Text = text
         };

         DiffContext.Line.Side side = new DiffContext.Line.Side
         {
            Number = linenumber,

            // this 'maker' cannot distinguish between modified and unmodified lines
            State = DiffContext.Line.State.Changed
         };

         if (isRightSideContext)
         {
            line.Right = side;
         }
         else
         {
            line.Left = side;
         }

         return line;
      }

      private readonly GitRepository _gitRepository;
   }
}
