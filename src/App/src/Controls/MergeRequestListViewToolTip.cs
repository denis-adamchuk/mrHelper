using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace mrHelper.App.Controls
{
   public partial class MergeRequestListViewToolTip : ToolTip
   {
      public MergeRequestListViewToolTip(ListView listView)
      {
         _listView = listView;

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

         ListViewHitTestInfo hit = _listView.HitTest(mouseLocation);
         CancelIfNeeded(hit);
         if (!isCellHit(hit))
         {
            return;
         }

         Debug.Assert(!isTooltipShown() || !isTooltipScheduled());
         Debug.Assert(_lastHistTestInfo == null || isTooltipShown() || isTooltipScheduled());
         Debug.Assert(_lastHistTestInfo == null || (_lastHistTestInfo.Item != null && _lastHistTestInfo.SubItem != null));

         bool hitAnotherCell() => _lastHistTestInfo.Item.Index  != hit.Item.Index
                               || _lastHistTestInfo.SubItem.Tag != hit.SubItem.Tag;

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

      public void CancelIfNeeded(ListViewHitTestInfo hit)
      {
         if (isCellHit(hit))
         {
            return;
         }

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
          || _lastHistTestInfo.SubItem.Tag == null
          || _lastHistTestInfo.Item == null
          || _lastHistTestInfo.Item.ListView == null)
         {
            return;
         }

         MergeRequestListViewSubItemInfo info = (MergeRequestListViewSubItemInfo)_lastHistTestInfo.SubItem.Tag;

         // shift tooltip position to the right of the cursor 16 pixels
         Point location = new Point(_lastMouseLocation.X + 16, _lastMouseLocation.Y);
         Show(info.TooltipText, _listView, location);
         _toolTipShown = true;
      }

      private void hideToolTip()
      {
         _toolTipShown = false;
         Hide(_listView);
      }

      private static bool isCellHit(ListViewHitTestInfo hit) => hit.Item != null && hit.SubItem != null;
      private bool isTooltipShown() => _toolTipShown;
      private bool isTooltipScheduled() => _toolTipScheduled;

      private bool _toolTipShown;
      private bool _toolTipScheduled;

      private readonly ListView _listView;
      private readonly System.Timers.Timer _toolTipTimer;

      private Point _lastMouseLocation = new Point(-1, -1);
      private ListViewHitTestInfo _lastHistTestInfo;
   }
}

