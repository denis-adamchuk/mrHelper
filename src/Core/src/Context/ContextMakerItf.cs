using System;
using System.Collections.Generic;
using System.Linq;
using mrHelper.Core.Matching;

namespace mrHelper.Core.Context
{
   /// <summary>
   /// Result of context makers work. Contains a list of strings with their properties.
   /// </summary>
   public struct DiffContext : IEquatable<DiffContext>
   {
      public DiffContext(IEnumerable<Line> lines, int selectedIndex)
      {
         Lines = lines;
         SelectedIndex = selectedIndex;
      }

      public static DiffContext InvalidContext => new DiffContext(null, 0);

      public bool IsValid() => !this.Equals(InvalidContext);

      public struct Line : IEquatable<Line>
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

         public struct Side : IEquatable<Side>
         {
            public Side(int number, State state)
            {
               Number = number;
               State = state;
            }

            public int Number { get; }
            public State State { get; }

            public override bool Equals(object obj)
            {
               return obj is Side side && Equals(side);
            }

            public bool Equals(Side other)
            {
               return Number == other.Number &&
                      State == other.State;
            }

            public override int GetHashCode()
            {
               int hashCode = 689241430;
               hashCode = hashCode * -1521134295 + Number.GetHashCode();
               hashCode = hashCode * -1521134295 + State.GetHashCode();
               return hashCode;
            }

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

         public override bool Equals(object obj)
         {
            return obj is Line line && Equals(line);
         }

         public bool Equals(Line other)
         {
            return Text == other.Text &&
                   EqualityComparer<Side?>.Default.Equals(Left, other.Left) &&
                   EqualityComparer<Side?>.Default.Equals(Right, other.Right);
         }

         public override int GetHashCode()
         {
            int hashCode = 976728275;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Text);
            hashCode = hashCode * -1521134295 + Left.GetHashCode();
            hashCode = hashCode * -1521134295 + Right.GetHashCode();
            return hashCode;
         }

         public string Text { get; }
         public Side? Left { get; }
         public Side? Right { get; }
      }

      public IEnumerable<Line> Lines { get; }

      public int SelectedIndex { get; }

      public override bool Equals(object obj)
      {
         return obj is DiffContext context && Equals(context);
      }

      public bool Equals(DiffContext other)
      {
         if (Lines == null && other.Lines == null)
         {
            return SelectedIndex == other.SelectedIndex;
         }
         else if (Lines != null && other.Lines != null)
         {
            return Enumerable.SequenceEqual(Lines, other.Lines) &&
                   SelectedIndex == other.SelectedIndex;
         }
         return false;
      }

      public override int GetHashCode()
      {
         throw new NotImplementedException();
      }
   }

   public struct ContextDepth : IEquatable<ContextDepth>
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

      public override bool Equals(object obj)
      {
         return obj is ContextDepth depth && Equals(depth);
      }

      public bool Equals(ContextDepth other)
      {
         return Up == other.Up &&
                Down == other.Down &&
                Size == other.Size;
      }

      public override int GetHashCode()
      {
         int hashCode = -1196061383;
         hashCode = hashCode * -1521134295 + Up.GetHashCode();
         hashCode = hashCode * -1521134295 + Down.GetHashCode();
         hashCode = hashCode * -1521134295 + Size.GetHashCode();
         return hashCode;
      }
   }

   public enum UnchangedLinePolicy
   {
      TakeFromLeft,
      TakeFromRight
   }

   public interface IContextMaker
   {
      DiffContext GetContext(DiffPosition position, ContextDepth depth, UnchangedLinePolicy unchangedLinePolicy);
   }
}

