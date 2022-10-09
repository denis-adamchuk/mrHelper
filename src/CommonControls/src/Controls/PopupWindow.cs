using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using mrHelper.CommonControls.Tools;

namespace mrHelper.CommonControls.Controls
{
   /// https://stackoverflow.com/a/282217
   /// <summary>
   /// A simple popup window that can host any System.Windows.Forms.Control
   /// </summary>
   public class PopupWindow : System.Windows.Forms.ToolStripDropDown, IDisposable
   {
      public PopupWindow(bool autoClose, int? borderRadius)
      {
         AutoSize = false;
         AutoClose = autoClose;
         DoubleBuffered = true;
         ResizeRedraw = true;
         _borderRadius = borderRadius;
      }

      protected override void Dispose(bool disposing)
      {
         dropSubscriptionOnSizeChange();
         base.Dispose(disposing);
      }

      private class MyToolStripControlHost : ToolStripControlHost
      {
         public MyToolStripControlHost(Control c) : base(c)
         {
            Margin = new Padding(0, 0, 0, 0);
         }
      }

      public void SetContent(System.Windows.Forms.Control content, Padding padding)
      {
         dropSubscriptionOnSizeChange();
         Items.Clear();

         if (content == null)
         {
            return;
         }

         Padding = padding;
         onResize(content);

         ToolStripControlHost host = new MyToolStripControlHost(content);
         Items.Add(host);

         content.SizeChanged += onHostedControlSizeChanged;
      }

      private Control getHostedControl()
      {
         if (Items.Count > 0 && Items[0] is MyToolStripControlHost hostedControl)
         {
            return hostedControl.Control;
         }
         return null;
      }

      private void dropSubscriptionOnSizeChange()
      {
         Control hostedControl = getHostedControl();
         if (hostedControl != null)
         {
            hostedControl.SizeChanged -= onHostedControlSizeChanged;
         }
      }

      private void onHostedControlSizeChanged(object sender, EventArgs e)
      {
         Control hostedControl = getHostedControl();
         if (hostedControl != null)
         {
            onResize(hostedControl);
         }
      }

      private void onResize(Control hostedControl)
      {
         System.Drawing.Size hostedControlSize = new System.Drawing.Size(
            hostedControl.Size.Width + Padding.Horizontal, hostedControl.Size.Height + Padding.Vertical);
         System.Drawing.Size hostedControlMinimumSize = new System.Drawing.Size(
            hostedControl.MinimumSize.Width + Padding.Horizontal, hostedControl.MinimumSize.Height + Padding.Vertical);

         MinimumSize = hostedControlMinimumSize;
         MaximumSize = hostedControlSize;
         Size = hostedControlSize;
      }

      private void recreateRegion()
      {
         Debug.Assert(_borderRadius.HasValue);
         using (System.Drawing.Drawing2D.GraphicsPath path = WinFormsHelpers.GetRoundedPath(
            ClientRectangle, _borderRadius.Value, HScroll))
         {
            Region = new Region(path);
         }
         Invalidate();
      }

      protected override void OnSizeChanged(EventArgs e)
      {
         base.OnSizeChanged(e);

         if (_prevSize != Size && _borderRadius.HasValue)
         {
            recreateRegion();
         }
         _prevSize = Size;
      }

      private readonly int? _borderRadius;
      private SizeF _prevSize = new SizeF();
   }
}

