using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrCore
{
   // This 'maker' create a list of lines belonging to the same side as the line passed to GetContext() call.
   // Unlike TextContextMaker this maker preserves state of each line: added/modified vs unchanged for right-side and
   // removed vs unchanged for left-side.
   //
   // Cost: one 'git show' and one 'git diff -U0' for each GetContext() call
   public class EnhancedContextMaker : ContextMaker
   {
      public EnhancedContextMaker(GitRepository gitRepository)
      {
         Debug.Assert(gitRepository != null);
         _gitRepository = gitRepository;
      }

      public DiffContext GetContext(Position position, ContextDepth depth)
      {
         if (!Context.Helpers.IsValidPosition(position) || !Context.Helpers.IsValidContextDepth(depth))
         {
            return new DiffContext();
         }

         // TODO Is it ok that we cannot handle different filenames?
         Debug.Assert(position.NewPath == position.OldPath);

         GitDiffAnalyzer analyzer =
            new GitDiffAnalyzer(_gitRepository, position.Refs.BaseSHA, position.Refs.HeadSHA, position.NewPath);

         // If NewLine is valid, then it points to either added/modified or unchanged line, handle them the same way
         bool isRightSideContext = position.NewLine != null;
         int linenumber = isRightSideContext ? int.Parse(position.NewLine) : int.Parse(position.OldLine);
         string filename = isRightSideContext ? position.NewPath : position.OldPath;
         string sha = isRightSideContext ? position.Refs.HeadSHA : position.Refs.BaseSHA;

         return createDiffContext(linenumber, filename, sha, isRightSideContext, analyzer, depth);
      }

      // isRightSideContext is true when linenumber and sha correspond to the right side
      // linenumber is one-based
      private DiffContext createDiffContext(int linenumber, string filename, string sha, bool isRightSideContext,
         GitDiffAnalyzer analyzer, ContextDepth depth)
      {
         DiffContext diffContext = new DiffContext();
         diffContext.Lines = new List<DiffContext.Line>();

         List<string> contents = _gitRepository.ShowFileByRevision(filename, sha);
         if (linenumber > contents.Count)
         {
            return new DiffContext();
         }

         int startLineNumber = Math.Max(1, linenumber - depth.Up);
         for (int iContextLine = 0; iContextLine < depth.Size + 1; ++iContextLine)
         {
            if (startLineNumber + iContextLine == contents.Count + 1)
            {
               // we have just reached the end
               break;
            }
            diffContext.Lines.Add(getLineContext(startLineNumber + iContextLine, isRightSideContext, analyzer, contents));
         }

         // zero-based index of a selected line in DiffContext.Lines
         diffContext.SelectedIndex = linenumber - startLineNumber;
         return diffContext;
      }

      // linenumber is one-based
      private DiffContext.Line getLineContext(int linenumber, bool isRightSideContext,
         GitDiffAnalyzer analyzer, List<string> contents)
      {
         Debug.Assert(linenumber > 0 && linenumber <= contents.Count);

         DiffContext.Line line = new DiffContext.Line();
         line.Text = contents[linenumber - 1];

         DiffContext.Line.Side side = new DiffContext.Line.Side();
         side.Number = linenumber;

         // this maker supports all three states
         side.State = getLineState(analyzer, linenumber, isRightSideContext);

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

      private readonly GitRepository _gitRepository;
   }
}
