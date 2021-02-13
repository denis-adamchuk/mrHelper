using System;
using static mrHelper.App.Helpers.ConfigurationHelper;

namespace mrHelper.App.Forms.Helpers
{
   public class DiscussionLayout
   {
      public DiscussionLayout(
         DiffContextPosition diffContextPosition,
         DiscussionColumnWidth discussionColumnWidth,
         bool needShiftReplies)
      {
         _diffContextPosition = diffContextPosition;
         _discussionColumnWidth = discussionColumnWidth;
         _needShiftReplies = needShiftReplies;
      }

      internal event Action DiffContextPositionChanged;
      internal event Action DiscussionColumnWidthChanged;
      internal event Action NeedShiftRepliesChanged;

      internal DiffContextPosition DiffContextPosition
      {
         get => _diffContextPosition;
         set
         {
            _diffContextPosition = value;
            DiffContextPositionChanged?.Invoke();
         }
      }

      internal DiscussionColumnWidth DiscussionColumnWidth
      {
         get => _discussionColumnWidth;
         set
         {
            _discussionColumnWidth = value;
            DiscussionColumnWidthChanged?.Invoke();
         }
      }

      internal bool NeedShiftReplies
      {
         get => _needShiftReplies;
         set
         {
            _needShiftReplies = value;
            NeedShiftRepliesChanged?.Invoke();
         }
      }

      private DiffContextPosition _diffContextPosition;
      private DiscussionColumnWidth _discussionColumnWidth;
      private bool _needShiftReplies;
   }
}

