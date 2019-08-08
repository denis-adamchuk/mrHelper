using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrHelper.Core.Context
{
   /// <summary>
   /// Result of context makers work. Contains a list of strings with their properties.
   /// </summary>
   public struct DiffContext
   {
      public struct Line
      {
         public enum State
         {
            Changed,
            Unchanged
         }

         public struct Side
         {
            public int Number;
            public State State;

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

         public string Text;
         public Side? Left;
         public Side? Right;
      }

      public List<Line> Lines;

      public int SelectedIndex;

      /*
      public string ToString()
      {
         string result = "\n";
         for (Line line in Lines)
         {
            result += line.ToString() + "\n";
         }
         result += String.Format("SelectedIndex: {0}", SelectedIndex.ToString());
      }
      */
   }

   public struct ContextDepth
   {
      public int Up;
      public int Down;

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

