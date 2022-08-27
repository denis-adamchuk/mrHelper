using System;
using System.Windows.Forms;

namespace mrHelper.CommonControls.Controls
{
   /// https://stackoverflow.com/a/282217
   /// <summary>
   /// A simple popup window that can host any System.Windows.Forms.Control
   /// </summary>
   public class PopupWindow : System.Windows.Forms.ToolStripDropDown
   {
      public PopupWindow(bool autoClose = false)
      {
         AutoSize = false;
         AutoClose = autoClose;
         DoubleBuffered = true;
         ResizeRedraw = true;
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
   }
}

