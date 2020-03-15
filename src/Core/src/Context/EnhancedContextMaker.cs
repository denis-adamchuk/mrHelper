using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using mrHelper.Core.Git;
using mrHelper.Core.Matching;
using mrHelper.Common.Interfaces;

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
      public EnhancedContextMaker(IGitRepository gitRepository)
      {
         Debug.Assert(gitRepository != null);
         _gitRepository = gitRepository;
      }

      /// <summary>
      /// Throws ArgumentException, ContextMakingException.
      /// </summary>
      async public Task<DiffContext> GetContext(DiffPosition position, ContextDepth depth)
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

         GitDiffAnalyzer analyzer = new GitDiffAnalyzer();
         await analyzer.AnalyzeAsync(_gitRepository, position.Refs.LeftSHA, position.Refs.RightSHA,
            position.LeftPath, position.RightPath);

         // If RightLine is valid, then it points to either added/modified or unchanged line, handle them the same way
         bool isRightSideContext = position.RightLine != null;
         int linenumber = isRightSideContext ? int.Parse(position.RightLine) : int.Parse(position.LeftLine);
         string filename = isRightSideContext ? position.RightPath : position.LeftPath;
         string sha = isRightSideContext ? position.Refs.RightSHA : position.Refs.LeftSHA;

         GitShowRevisionArguments arguments = new GitShowRevisionArguments
         {
            Filename = filename,
            Sha = sha
         };

         IEnumerable<string> gitResult;
         try
         {
            gitResult = await _gitRepository.Data?.GetAsync(arguments);
         }
         catch (GitNotAvailableDataException ex)
         {
            throw new ContextMakingException("Cannot obtain git revision", ex);
         }

         if (gitResult == null)
         {
            throw new ContextMakingException("Cannot obtain git revision", null);
         }

         string[] contents = gitResult.ToArray();
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
      private DiffContext createDiffContext(int linenumber, bool isRightSideContext, string[] contents,
         GitDiffAnalyzer analyzer, ContextDepth depth)
      {
         DiffContext diffContext = new DiffContext
         {
            Lines = new List<DiffContext.Line>()
         };

         int startLineNumber = Math.Max(1, linenumber - depth.Up);
         for (int iContextLine = 0; iContextLine < depth.Size + 1; ++iContextLine)
         {
            if (startLineNumber + iContextLine == contents.Count() + 1)
            {
               // we have just reached the end
               break;
            }
            diffContext.Lines.Add(getLineContext(
               startLineNumber + iContextLine, isRightSideContext, analyzer, contents));
         }

         // zero-based index of a selected line in DiffContext.Lines
         diffContext.SelectedIndex = linenumber - startLineNumber;
         return diffContext;
      }

      // linenumber is one-based
      private DiffContext.Line getLineContext(int linenumber, bool isRightSideContext,
         GitDiffAnalyzer analyzer, string[] contents)
      {
         Debug.Assert(linenumber > 0 && linenumber <= contents.Count());

         DiffContext.Line line = new DiffContext.Line
         {
            Text = contents[linenumber - 1]
         };

         DiffContext.Line.Side side = new DiffContext.Line.Side
         {
            Number = linenumber,

            // this maker supports all three states
            State = getLineState(analyzer, linenumber, isRightSideContext)
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

      private readonly IGitRepository _gitRepository;
   }
}
