using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using static System.Windows.Forms.ListViewItem;
using mrHelper.CommonControls.Tools;
using mrHelper.CommonNative;

namespace mrHelper.App.Controls
{
   public partial class ListViewToolTip : ToolTip
   {
      public ListViewToolTip(ListView listView,
         Func<ListViewSubItem, string> getText,
         Func<ListViewSubItem, string> getToolTipText,
         Func<ListViewSubItem, StringFormatFlags> getFormatFlags,
         Func<ListViewSubItem, Rectangle> getBounds,
         Func<ListViewSubItem, bool> getForceShowToolTip)
      {
         _listView = listView;
         _getText = getText;
         _getToolTipText = getToolTipText;
         _getFormatFlags = getFormatFlags;
         _getBounds = getBounds;
         _getForceShowToolTip = getForceShowToolTip;

         _toolTipTimer = new System.Timers.Timer
         {
            Interval = 500,
            AutoReset = false,
            SynchronizingObject = listView
         };

         _toolTipTimer.Elapsed += (s, et) => onToolTipTimer();
      }

      /// <summary>
      /// Clean up any resources being used.
      /// </summary>
      /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
      protected override void Dispose(bool disposing)
      {
         _toolTipTimer?.Stop();
         _toolTipTimer?.Dispose();
         base.Dispose(disposing);
      }

      public void UpdateOnMouseMove(Point mouseLocation)
      {
         if (_lastMouseLocation == mouseLocation)
         {
            return;
         }
         _lastMouseLocation = mouseLocation;

         CancelIfNeeded(_listView.PointToScreen(mouseLocation));
         ListViewHitTestInfo hit = _listView.HitTest(mouseLocation);
         if (!isAnyCellHit(hit))
         {
            return;
         }

         Debug.Assert(!isTooltipShown() || !isTooltipScheduled());
         Debug.Assert(_lastHistTestInfo == null || isTooltipShown() || isTooltipScheduled());
         Debug.Assert(_lastHistTestInfo == null || (_lastHistTestInfo.Item != null && _lastHistTestInfo.SubItem != null));

         bool hitAnotherCell() => _lastHistTestInfo.Item.Index  != hit.Item.Index
                               || _lastHistTestInfo.SubItem != hit.SubItem;

         if (isTooltipShown())
         {
            if (hitAnotherCell())
            {
               startToolTipTimer();
               hideToolTip();
               _lastHistTestInfo = hit;
            }
         }
         else if (isTooltipScheduled())
         {
            if (hitAnotherCell())
            {
               _lastHistTestInfo = hit;
            }
         }
         else
         {
            startToolTipTimer();
            _lastHistTestInfo = hit;
         }
      }

      // optimization: header height obtaining requires a sync call
      private int _headerHeight = 0;

      public void CancelIfNeeded(Point screenPosition)
      {
         // optimization -- see conditions in Cancel()
         if (_headerHeight == 0)
         {
            _headerHeight = Win32Tools.GetListViewHeaderHeight(_listView.Handle);
         }

         bool atHeader = _listView.PointToClient(screenPosition).Y <= _headerHeight;
         bool atListView = isAnyCellHit(_listView.HitTest(_listView.PointToClient(screenPosition)));
         if (atListView && !atHeader)
         {
            // don't cancel when screenPosition is within ListView (except its Header part)
            return;
         }

         Cancel();
      }

      public void Cancel()
      {
         if (isTooltipScheduled())
         {
            stopToolTipTimer();
         }

         if (isTooltipShown())
         {
            hideToolTip();
         }

         _lastHistTestInfo = null;
      }

      private void startToolTipTimer()
      {
         _toolTipTimer.Start();
         _toolTipScheduled = true;
      }

      private void stopToolTipTimer()
      {
         _toolTipScheduled = false;
         _toolTipTimer.Stop();
      }

      private void onToolTipTimer()
      {
         _toolTipScheduled = false;

         if (_lastHistTestInfo == null
          || _lastHistTestInfo.SubItem == null
          || _lastHistTestInfo.Item == null
          || _lastHistTestInfo.Item.ListView == null)
         {
            return;
         }

         // shift tooltip position to the right of the cursor 16 pixels
         Point location = new Point(_lastMouseLocation.X + 16, _lastMouseLocation.Y);
         string text = needShowToolTip(_lastHistTestInfo.SubItem) ? _getToolTipText(_lastHistTestInfo.SubItem) : null;
         Show(text, _listView, location);
         _toolTipShown = true;
      }

      private void hideToolTip()
      {
         _toolTipShown = false;
         Hide(_listView);
      }

      private bool needShowToolTip(ListViewItem.ListViewSubItem subItem)
      {
         if (_getForceShowToolTip(subItem))
         {
            return true;
         }

         string text = _getText(subItem);
         StringFormatFlags formatFlags = _getFormatFlags(subItem);
         Rectangle bounds = _getBounds(subItem);
         Graphics graphics = _listView.CreateGraphics();

         StringFormat formatTrimmed = new StringFormat(formatFlags)
         {
            Trimming = StringTrimming.EllipsisCharacter
         };
         SizeF textTrimmedSize = graphics.MeasureString(text, _listView.Font, bounds.Size, formatTrimmed);

         StringFormat formatFull = new StringFormat(formatFlags)
         {
            Trimming = StringTrimming.None
         };
         SizeF textFullSize = graphics.MeasureString(text, _listView.Font, bounds.Size, formatFull);

         bool exceedsWidth = textTrimmedSize.Width != textFullSize.Width; //-V3024
         bool exceedsHeight = textTrimmedSize.Height != textFullSize.Height; //-V3024
         return exceedsWidth || exceedsHeight;
      }

      private static bool isAnyCellHit(ListViewHitTestInfo hit) => hit.Item != null && hit.SubItem != null;
      private bool isTooltipShown() => _toolTipShown;
      private bool isTooltipScheduled() => _toolTipScheduled;

      private bool _toolTipShown;
      private bool _toolTipScheduled;

      private readonly ListView _listView;
      private readonly Func<ListViewSubItem, string> _getText;
      private readonly Func<ListViewSubItem, string> _getToolTipText;
      private readonly Func<ListViewSubItem, StringFormatFlags> _getFormatFlags;
      private readonly Func<ListViewSubItem, Rectangle> _getBounds;
      private readonly Func<ListViewSubItem, bool> _getForceShowToolTip;
      private readonly System.Timers.Timer _toolTipTimer;

      private Point _lastMouseLocation = new Point(-1, -1);
      private ListViewHitTestInfo _lastHistTestInfo;
   }
}

