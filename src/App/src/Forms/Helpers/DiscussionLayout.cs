using System;
using mrHelper.Core.Context;
using static mrHelper.App.Helpers.ConfigurationHelper;

namespace mrHelper.App.Forms.Helpers
{
   internal class DiscussionLayout
   {
      public DiscussionLayout(
         DiffContextPosition diffContextPosition,
         DiscussionColumnWidth discussionColumnWidth,
         bool needShiftReplies,
         ContextDepth diffContextDepth)
      {
         _diffContextPosition = diffContextPosition;
         _discussionColumnWidth = discussionColumnWidth;
         _needShiftReplies = needShiftReplies;
         _diffContextDepth = diffContextDepth;
      }

      internal event Action DiffContextPositionChanged;
      internal event Action DiscussionColumnWidthChanged;
      internal event Action NeedShiftRepliesChanged;
      internal event Action DiffContextDepthChanged;

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

      internal ContextDepth DiffContextDepth
      {
         get => _diffContextDepth;
         set
         {
            _diffContextDepth = value;
            DiffContextDepthChanged?.Invoke();
         }
      }

      private DiffContextPosition _diffContextPosition;
      private DiscussionColumnWidth _discussionColumnWidth;
      private bool _needShiftReplies;
      private ContextDepth _diffContextDepth;
   }
}

