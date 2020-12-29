using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using mrHelper.Core.Matching;
using mrHelper.StorageSupport;

namespace mrHelper.Core.Context
{
   /// <summary>
   /// This 'maker' creates a list of lines for either left or right side.
   /// The list starts from a line that is passed to GetContext() method and contains 'size' lines from the same side as
   /// the first line. If the first line is 'unmodified' then resulting list contains 'size' lines from the right side.
   ///
   // Cost: one 'git show' command for each GetContext() call.
   /// </summary>
   public class SimpleContextMaker : IContextMaker
   {
      public SimpleContextMaker(IGitCommandService git)
      {
         Debug.Assert(git != null);
         _git = git;
      }

      /// <summary>
      /// Throws ArgumentException, ContextMakingException.
      /// </summary>
      public DiffContext GetContext(DiffPosition position, ContextDepth depth, UnchangedLinePolicy unchangedLinePolicy)
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

         bool isRightSideContext = Helpers.IsRightSidePosition(position, unchangedLinePolicy);
         int linenumber = isRightSideContext ? Helpers.GetRightLineNumber(position) : Helpers.GetLeftLineNumber(position);
         string filename = isRightSideContext ? position.RightPath : position.LeftPath;
         string sha = isRightSideContext ? position.Refs.RightSHA : position.Refs.LeftSHA;

         GitShowRevisionArguments arguments = new GitShowRevisionArguments(filename, sha);

         IEnumerable<string> contents;
         try
         {
            contents = _git?.ShowRevision(arguments);
         }
         catch (GitNotAvailableDataException ex)
         {
            throw new ContextMakingException("Cannot obtain git revision", ex);
         }

         if (contents == null)
         {
            throw new ContextMakingException("Cannot obtain git revision", null);
         }

         if (linenumber > contents.Count())
         {
            throw new ArgumentException(
               String.Format("Line number {0} is greater than total line number count, invalid \"position\": {1}",
               linenumber.ToString(), position.ToString()));
         }

         return createDiffContext(linenumber, contents, isRightSideContext, depth);
      }

      // isRightSideContext is true when linenumber and sha correspond to the right side
      // linenumber is one-based
      private DiffContext createDiffContext(int linenumber, IEnumerable<string> contents, bool isRightSideContext,
         ContextDepth depth)
      {
         int startLineNumber = Math.Max(1, linenumber - depth.Up);
         int selectedIndex = linenumber - startLineNumber;
         List<DiffContext.Line> lines = new List<DiffContext.Line>();

         IEnumerable<string> shiftedContents = contents.Skip(startLineNumber - 1);
         foreach (string text in shiftedContents)
         {
            lines.Add(getContextLine(startLineNumber + lines.Count, isRightSideContext, text));
            if (lines.Count == depth.Size + 1)
            {
               break;
            }
         }

         // zero-based index of a selected line in DiffContext.Lines
         return new DiffContext(lines, selectedIndex);
      }

      // linenumber is one-based
      private static DiffContext.Line getContextLine(int linenumber, bool isRightSideContext, string text)
      {
         // this 'maker' cannot distinguish between modified and unmodified lines
         DiffContext.Line.Side side = new DiffContext.Line.Side(linenumber, DiffContext.Line.State.Changed);
         return new DiffContext.Line(text,
            isRightSideContext ? new DiffContext.Line.Side?() : side,
            isRightSideContext ? side : new DiffContext.Line.Side?());
      }

      private readonly IGitCommandService _git;
   }
}

