using System.Windows.Forms;

namespace mrHelper.App.Controls
{
   /// https://stackoverflow.com/a/282217
   /// <summary>
   /// A simple popup window that can host any System.Windows.Forms.Control
   /// </summary>
   public class PopupWindow : System.Windows.Forms.ToolStripDropDown
   {
      public PopupWindow(System.Windows.Forms.Control content, Padding padding)
      {
         AutoSize = false;
         AutoClose = false;
         DoubleBuffered = true;
         ResizeRedraw = true;
         Padding = padding;

         MinimumSize = new System.Drawing.Size(
            content.MinimumSize.Width + padding.Horizontal,
            content.MinimumSize.Height + padding.Vertical);
         MaximumSize = new System.Drawing.Size(
            content.Size.Width + padding.Horizontal,
            content.Size.Height + padding.Vertical);
         Size = new System.Drawing.Size(
            content.Size.Width + padding.Horizontal,
            content.Size.Height + padding.Vertical);

         ToolStripControlHost host = new System.Windows.Forms.ToolStripControlHost(content);
         Items.Add(host);
      }
   }
}

