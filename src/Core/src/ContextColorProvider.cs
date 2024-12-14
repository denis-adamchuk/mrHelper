using System.Drawing;

namespace mrHelper.Core
{
   public struct ContextColorProvider
   {
      public ContextColorProvider(
         Color lineNumbersColor, Color lineNumbersBackgroundColor, Color lineNumbersRightBorderColor,
         Color unchangedColor, Color unchangedBackgroundColor,
         Color addedColor, Color addedBackgroundColor,
         Color removedColor, Color removedBackgroundColor)
      {
         LineNumbersColor = lineNumbersColor;
         LineNumbersBackgroundColor = lineNumbersBackgroundColor;
         LineNumbersRightBorderColor = lineNumbersRightBorderColor;
         UnchangedColor = unchangedColor;
         UnchangedBackgroundColor = unchangedBackgroundColor;
         AddedColor = addedColor;
         AddedBackgroundColor = addedBackgroundColor;
         RemovedColor = removedColor;
         RemovedBackgroundColor = removedBackgroundColor;
      }

      internal Color LineNumbersColor { get; }

      internal Color LineNumbersBackgroundColor { get; }

      internal Color LineNumbersRightBorderColor { get; }

      internal Color UnchangedColor { get; }

      internal Color UnchangedBackgroundColor { get; }

      internal Color AddedColor { get; }

      internal Color AddedBackgroundColor { get; }

      internal Color RemovedColor { get; }

      internal Color RemovedBackgroundColor { get; }
   }
}
