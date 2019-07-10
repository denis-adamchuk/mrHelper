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
   public class PlainContextMaker : ContextMaker
   {
      public PlainContextMaker(GitRepository gitRepository)
      {
         Debug.Assert(gitRepository != null);
         _gitRepository = gitRepository;
      }

      public DiffContext GetContext(Position position, int size)
      {
         Debug.Assert(position.OldLine != null || position.NewLine != null);

         bool isRightSideContext = position.NewLine != null;
         int linenumber = isRightSideContext ? int.Parse(position.NewLine) : int.Parse(position.OldLine);
         string filename = isRightSideContext ? position.NewPath : position.OldPath;
         string sha = isRightSideContext ? position.Refs.HeadSHA : position.Refs.BaseSHA;

         return createDiffContext(linenumber, filename, sha, isRightSideContext, size);
      }

      // isRightSideContext is true when linenumber and sha correspond to the right side
      private DiffContext createDiffContext(int linenumber, string filename, string sha, bool isRightSideContext, int size)
      {
         DiffContext diffContext = new DiffContext();
         diffContext.Lines = new List<DiffContext.Line>();

         List<string> contents = _gitRepository.ShowFileByRevision(filename, sha);
         if (linenumber <= 0 || linenumber > contents.Count)
         {
            Debug.Assert(false);
            return new DiffContext();
         }

         IEnumerable<string> shiftedContents = contents.Skip(linenumber - 1);
         foreach (string text in shiftedContents)
         {
            diffContext.Lines.Add(getContextLine(linenumber + diffContext.Lines.Count, isRightSideContext, text));
            if (diffContext.Lines.Count == size)
            {
               break;
            }
         }

         diffContext.SelectedIndex = 0;

         return diffContext;
      }

      private static DiffContext.Line getContextLine(int linenumber, bool isRightSideContext, string text)
      {
         DiffContext.Line line = new DiffContext.Line();
         line.Text = text;

         DiffContext.Line.Side side = new DiffContext.Line.Side();
         side.Number = linenumber;
         
         // this 'maker' cannot distinguish between modified and unmodified lines
         side.State = DiffContext.Line.State.Changed;

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

