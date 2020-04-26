using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using mrHelper.CommonControls.Controls;

namespace mrHelper.App.Controls
{
   public partial class MergeRequestListView : ListViewEx
   {
      public MergeRequestListView()
      {
         InitializeComponent();

         toolTipTimer = new System.Timers.Timer
         {
            Interval = 500,
            AutoReset = false,
            SynchronizingObject = this
         };

         toolTipTimer.Elapsed +=
            (s, et) =>
         {
            if (lastHistTestInfo == null
             || lastHistTestInfo.SubItem == null
             || lastHistTestInfo.SubItem.Tag == null)
            {
               Debug.Assert(false);
               return;
            }

            ListViewSubItemInfo info = (ListViewSubItemInfo)lastHistTestInfo.SubItem.Tag;

            // shift tooltip position to the right of the cursor 16 pixels
            Point location = new Point(lastMouseLocation.X + 16, lastMouseLocation.Y);
            toolTip.Show(info.TooltipText, lastHistTestInfo.Item.ListView, location);
         };
      }

      public struct ListViewSubItemInfo
      {
         public ListViewSubItemInfo(Func<bool, string> getText, Func<string> getUrl)
         {
            _getText = getText;
            _getUrl = getUrl;
         }

         public bool Clickable => _getUrl() != String.Empty;
         public string Text => _getText(false);
         public string Url => _getUrl();
         public string TooltipText
         {
            get
            {
               return !String.IsNullOrWhiteSpace(Url) ? Url : _getText(true);
            }
         }

         private readonly Func<bool, string> _getText;
         private readonly Func<string> _getUrl;
      }

      protected override void OnMouseLeave(EventArgs e)
      {
         onLeave();
         base.OnMouseLeave(e);
      }

      protected override void OnMouseMove(MouseEventArgs e)
      {
         ListViewHitTestInfo hit = HitTest(e.Location);

         if (hit.Item == null || hit.SubItem == null)
         {
            onLeave();
            return;
         }

         if (lastMouseLocation == e.Location)
         {
            return;
         }
         lastMouseLocation = e.Location;

         if (!String.IsNullOrEmpty(toolTip.GetToolTip(this)))
         {
            Debug.Assert(!toolTipTimer.Enabled);
            if (lastHistTestInfo == null
             || lastHistTestInfo.Item == null
             || lastHistTestInfo.SubItem == null
             || lastHistTestInfo.Item.Index != hit.Item.Index
             || lastHistTestInfo.SubItem.Tag != hit.SubItem.Tag)
            {
               toolTip.Hide(this);
               lastHistTestInfo = hit;
               toolTipTimer.Start();
            }
         }
         else
         {
            if (lastHistTestInfo == null
             || lastHistTestInfo.Item == null
             || lastHistTestInfo.SubItem == null
             || lastHistTestInfo.Item.Index != hit.Item.Index
             || lastHistTestInfo.SubItem.Tag != hit.SubItem.Tag)
            {
               if (toolTipTimer.Enabled)
               {
                  toolTipTimer.Stop();
               }
               lastHistTestInfo = hit;
               toolTipTimer.Start();
            }
         }

         base.OnMouseMove(e);
      }

      private void onLeave()
      {
         if (toolTipTimer.Enabled)
         {
            toolTipTimer.Stop();
         }

         if (!String.IsNullOrEmpty(toolTip.GetToolTip(this)))
         {
            toolTip.Hide(this);
         }

         lastHistTestInfo = null;
      }

      private readonly ToolTip toolTip = new ToolTip();
      private System.Timers.Timer toolTipTimer;
      private Point lastMouseLocation = new Point(-1, -1);
      private ListViewHitTestInfo lastHistTestInfo;
   }
}

