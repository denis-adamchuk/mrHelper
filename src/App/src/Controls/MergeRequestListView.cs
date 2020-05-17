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

         _toolTipTimer = new System.Timers.Timer
         {
            Interval = 500,
            AutoReset = false,
            SynchronizingObject = this
         };

         _toolTipTimer.Elapsed +=
            (s, et) =>
         {
            if (_lastHistTestInfo == null
             || _lastHistTestInfo.SubItem == null
             || _lastHistTestInfo.SubItem.Tag == null
             || _lastHistTestInfo.Item == null
             || _lastHistTestInfo.Item.ListView == null)
            {
               return;
            }

            ListViewSubItemInfo info = (ListViewSubItemInfo)_lastHistTestInfo.SubItem.Tag;

            // shift tooltip position to the right of the cursor 16 pixels
            Point location = new Point(_lastMouseLocation.X + 16, _lastMouseLocation.Y);
            _toolTip.Show(info.TooltipText, _lastHistTestInfo.Item.ListView, location);
         };

         // had to use this hack, because it is not possible to prevent deselecting a row
         // on a click on empty area in ListView
         _delayedDeselectionTimer = new System.Windows.Forms.Timer
         {
            // using a very short Interval to emulate a quick deselection on clicking an empty area
            Interval = 100,
         };
         _delayedDeselectionTimer.Tick +=
            (s, ee) =>
         {
            _delayedDeselectionTimer.Stop();
            Deselected?.Invoke(this);
         };
      }

      public class ListViewSubItemInfo
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

      public event Action<ListView> Deselected;

      protected override void OnItemSelectionChanged(ListViewItemSelectionChangedEventArgs e)
      {
         Refresh();

         if (SelectedItems.Count < 1)
         {
            _delayedDeselectionTimer.Start();
            return;
         }

         if (_delayedDeselectionTimer.Enabled)
         {
            _delayedDeselectionTimer.Stop();
         }

         base.OnItemSelectionChanged(e);
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

         if (_lastMouseLocation == e.Location)
         {
            return;
         }
         _lastMouseLocation = e.Location;

         if (!String.IsNullOrEmpty(_toolTip.GetToolTip(this)))
         {
            Debug.Assert(!_toolTipTimer.Enabled);
            if (_lastHistTestInfo == null
             || _lastHistTestInfo.Item == null
             || _lastHistTestInfo.SubItem == null
             || _lastHistTestInfo.Item.Index != hit.Item.Index
             || _lastHistTestInfo.SubItem.Tag != hit.SubItem.Tag)
            {
               _toolTip.Hide(this);
               _lastHistTestInfo = hit;
               _toolTipTimer.Start();
            }
         }
         else
         {
            if (_lastHistTestInfo == null
             || _lastHistTestInfo.Item == null
             || _lastHistTestInfo.SubItem == null
             || _lastHistTestInfo.Item.Index != hit.Item.Index
             || _lastHistTestInfo.SubItem.Tag != hit.SubItem.Tag)
            {
               if (_toolTipTimer.Enabled)
               {
                  _toolTipTimer.Stop();
               }
               _lastHistTestInfo = hit;
               _toolTipTimer.Start();
            }
         }

         base.OnMouseMove(e);
      }

      private void onLeave()
      {
         if (_toolTipTimer.Enabled)
         {
            _toolTipTimer.Stop();
         }

         if (!String.IsNullOrEmpty(_toolTip.GetToolTip(this)))
         {
            _toolTip.Hide(this);
         }

         _lastHistTestInfo = null;
      }

      private readonly ToolTip _toolTip = new ToolTip();
      private System.Timers.Timer _toolTipTimer;
      private Point _lastMouseLocation = new Point(-1, -1);
      private ListViewHitTestInfo _lastHistTestInfo;

      // Using System.Windows.Forms.Timer here because it remains Enabled
      // if even Interval exceeded between Start() and Stop() calls occurred
      // within a single execution thread without async processing.
      // System.Timers.Timer behaves differently. If Interval exceeds
      // between Start() and Stop() (see OnItemSelectionChanged),
      // Enabled property is reset and a timer event is already queued so
      // it will trigger when no longer needed.
      private System.Windows.Forms.Timer _delayedDeselectionTimer;
   }
}

