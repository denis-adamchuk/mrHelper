using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mrHelper.Core.Matching;

namespace mrHelper.Core.Context
{
   /// <summary>
   /// Result of context makers work. Contains a list of strings with their properties.
   /// </summary>
   public struct DiffContext
   {
      public DiffContext(IEnumerable<Line> lines, int selectedIndex)
      {
         Lines = lines;
         SelectedIndex = selectedIndex;
      }

      public struct Line
      {
         public Line(string text, Side? left, Side? right)
         {
            Text = text;
            Left = left;
            Right = right;
         }

         public enum State
         {
            Changed,
            Unchanged
         }

         public struct Side
         {
            public Side(int number, State state)
            {
               Number = number;
               State = state;
            }

            public int Number { get; }
            public State State { get; }

            new public string ToString()
            {
               return String.Format("\nNumber: {0}\nState: {1}", Number.ToString(), State.ToString());
            }
         }

         new public string ToString()
         {
            return String.Format("\nText: {0}\nLeft: {1}\nRight: {2}",
               Text, (Left?.ToString() ?? "null"), (Right?.ToString() ?? "null"));
         }

         public string Text { get; }
         public Side? Left { get; }
         public Side? Right { get; }
      }

      public IEnumerable<Line> Lines { get; }

      public int SelectedIndex { get; }
   }

   public struct ContextDepth
   {
      public int Up { get; }
      public int Down { get; }

      public int Size
      {
         get { return Up + Down; }
      }

      public ContextDepth(int up, int down)
      {
         Up = up;
         Down = down;
      }

      public override string ToString()
      {
         return String.Format("\nUp: {0}\nDown: {1}", Up.ToString(), Down.ToString());
      }
   }

   public interface IContextMaker
   {
      DiffContext GetContext(DiffPosition position, ContextDepth depth);
   }
}

