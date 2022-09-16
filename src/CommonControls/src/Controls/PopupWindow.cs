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
   public class PopupWindow : System.Windows.Forms.ToolStripDropDown
   {
      public PopupWindow(bool autoClose, int? borderRadius)
      {
         AutoSize = false;
         AutoClose = autoClose;
         DoubleBuffered = true;
         ResizeRedraw = true;
         _borderRadius = borderRadius;
      }

      private class MyToolStripControlHost : ToolStripControlHost
      {
         public MyToolStripControlHost(Control c, Action<Control> onHostedControlResize) : base(c)
         {
            _onHostedControlResize = onHostedControlResize;
         }

         protected override void OnHostedControlResize(EventArgs e)
         {
            base.OnHostedControlResize(e);
            _onHostedControlResize?.Invoke(Control);
         }

         private Action<Control> _onHostedControlResize;
      }

      public void SetContent(System.Windows.Forms.Control content, Padding padding)
      {
         Items.Clear();

         Padding = padding;
         onResize(content);

         ToolStripControlHost host = new MyToolStripControlHost(content, onResize);
         Items.Add(host);
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
         int radius = _borderRadius.Value;
         using (System.Drawing.Drawing2D.GraphicsPath path =
            WinFormsHelpers.GetRoundRectagle(ClientRectangle, radius, HScroll))
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

