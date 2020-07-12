using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using mrHelper.Core.Git;
using mrHelper.Core.Matching;
using mrHelper.StorageSupport;

namespace mrHelper.Core.Context
{
   /// <summary>
   /// This 'maker' create a list of lines belonging to the same side as the line passed to GetContext() call.
   /// Unlike TextContextMaker this maker preserves state of each line: added/modified vs unchanged for right-side and
   /// removed vs unchanged for left-side.
   ///
   /// Cost: one 'git show' and one 'git diff -U0' for each GetContext() call
   /// </summary>
   public class EnhancedContextMaker : IContextMaker
   {
      public EnhancedContextMaker(IGitCommandService git)
      {
         Debug.Assert(git != null);
         _git = git;
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

         GitDiffAnalyzer analyzer = new GitDiffAnalyzer(_git,
            position.Refs.LeftSHA, position.Refs.RightSHA, position.LeftPath, position.RightPath);

         // If RightLine is valid, then it points to either added/modified or unchanged line, handle them the same way
         bool isRightSideContext = position.RightLine != null;
         int linenumber = isRightSideContext ? int.Parse(position.RightLine) : int.Parse(position.LeftLine);
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

         return createDiffContext(linenumber, isRightSideContext, contents, analyzer, depth);
      }

      // isRightSideContext is true when linenumber and sha correspond to the right side
      // linenumber is one-based
      private DiffContext createDiffContext(int linenumber, bool isRightSideContext, IEnumerable<string> contents,
         GitDiffAnalyzer analyzer, ContextDepth depth)
      {
         List<DiffContext.Line> lines = new List<DiffContext.Line>();

         int startLineNumber = Math.Max(1, linenumber - depth.Up);
         for (int iContextLine = 0; iContextLine < depth.Size + 1; ++iContextLine)
         {
            if (startLineNumber + iContextLine == contents.Count() + 1)
            {
               // we have just reached the end
               break;
            }
            lines.Add(getLineContext(
               startLineNumber + iContextLine, isRightSideContext, analyzer, contents));
         }

         // zero-based index of a selected line in DiffContext.Lines
         return new DiffContext(lines, linenumber - startLineNumber);
      }

      // linenumber is one-based
      private DiffContext.Line getLineContext(int linenumber, bool isRightSideContext,
         GitDiffAnalyzer analyzer, IEnumerable<string> contents)
      {
         Debug.Assert(linenumber > 0 && linenumber <= contents.Count());

         // this maker supports all three states
         DiffContext.Line.Side side = new DiffContext.Line.Side(
            linenumber, getLineState(analyzer, linenumber, isRightSideContext));

         return new DiffContext.Line(contents.ElementAt(linenumber - 1),
            isRightSideContext ? new DiffContext.Line.Side?() : side,
            isRightSideContext ? side : new DiffContext.Line.Side?());
      }

      // linenumber is one-based
      private DiffContext.Line.State getLineState(GitDiffAnalyzer analyzer, int linenumber, bool isRightSideContext)
      {
         if (isRightSideContext)
         {
            return analyzer.IsLineAddedOrModified(linenumber)
               ? DiffContext.Line.State.Changed : DiffContext.Line.State.Unchanged;
         }
         else
         {
            return analyzer.IsLineDeleted(linenumber)
               ? DiffContext.Line.State.Changed : DiffContext.Line.State.Unchanged;
         }
      }

      private readonly IGitCommandService _git;
   }
}
