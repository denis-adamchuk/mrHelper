using mrHelper.App.Controls;

namespace mrHelper.App.Helpers
{
   public struct TextSearchResult
   {
      public TextSearchResult(ITextControl control, int insideControlPosition)
      {
         Control = control;
         InsideControlPosition = insideControlPosition;
      }

      public ITextControl Control { get; }
      public int InsideControlPosition { get; }
   }
}

