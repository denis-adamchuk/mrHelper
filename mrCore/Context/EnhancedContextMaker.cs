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
         _gitRepository = gitRepository;
      }

      public DiffContext GetContext(Position position, int size)
      {
         // TODO Is it ok that we cannot handle different filenames?
         Debug.Assert(position.NewPath == position.OldPath);
         Debug.Assert(position.NewLine != null || position.OldLine != null);

         GitDiffAnalyzer analyzer =
            new GitDiffAnalyzer(_gitRepository, position.Refs.BaseSHA, position.Refs.HeadSHA, position.NewPath);

         // If NewLine is valid, then it points to either added/modified or unchanged line, handle them the same way
         bool isRightSideContext = position.NewLine != null;
         int linenumber = isRightSideContext ? int.Parse(position.NewLine) : int.Parse(position.OldLine);
         string filename = isRightSideContext ? position.NewPath : position.OldPath;
         string sha = isRightSideContext ? position.Refs.HeadSHA : position.Refs.BaseSHA;

         return createDiffContext(linenumber, filename, sha, isRightSideContext, analyzer, size);
      }

      // isRightSideContext is true when linenumber and sha correspond to the right side
      private DiffContext createDiffContext(int linenumber, string filename, string sha, bool isRightSideContext,
         GitDiffAnalyzer analyzer, int size)
      {
         DiffContext diffContext = new DiffContext();
         diffContext.Lines = new List<DiffContext.Line>();

         List<string> contents = _gitRepository.ShowFileByRevision(filename, sha);
         if (linenumber <= 0 || linenumber > contents.Count)
         {
            Debug.Assert(false);
            return new DiffContext();
         }

         diffContext.Lines.Add(getLineContext(linenumber, isRightSideContext, analyzer, contents));

         for (int iContextLine = 1; iContextLine < size; ++iContextLine)
         {
            if (linenumber + iContextLine == contents.Count + 1)
            {
               // we have just reached the end
               break;
            }
            diffContext.Lines.Add(getLineContext(linenumber + iContextLine, isRightSideContext, analyzer, contents));
         }

         return diffContext;
      }

      private DiffContext.Line getLineContext(int linenumber, bool isRightSideContext,
         GitDiffAnalyzer analyzer, List<string> contents)
      {
         Debug.Assert(linenumber > 0 && linenumber <= contents.Count);

         DiffContext.Line line = new DiffContext.Line();
         line.Text = contents[linenumber - 1];
         line.Sides = new List<DiffContext.Line.Side>();

         DiffContext.Line.Side side = new DiffContext.Line.Side();
         side.State = getLineState(analyzer, linenumber, isRightSideContext);
         side.Right = isRightSideContext;
         side.Number = linenumber;

         line.Sides.Add(side);
         return line;
      }

      private DiffContext.Line.State getLineState(GitDiffAnalyzer analyzer, int linenumber, bool isRightSideContext)
      {
         if (isRightSideContext)
         {
            return analyzer.IsLineAddedOrModified(linenumber)
               ? DiffContext.Line.State.Added : DiffContext.Line.State.Unchanged;
         }
         else
         {
            return analyzer.IsLineDeleted(linenumber)
               ? DiffContext.Line.State.Removed : DiffContext.Line.State.Unchanged;
         }
      }

      private readonly GitRepository _gitRepository;
   }
}
