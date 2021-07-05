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

      public void SetContent(System.Windows.Forms.Control content, Padding padding)
      {
         Items.Clear();

         Padding = padding;

         System.Drawing.Size contentSize = new System.Drawing.Size(
            content.Size.Width + padding.Horizontal, content.Size.Height + padding.Vertical);
         System.Drawing.Size contentMinimumSize = new System.Drawing.Size(
            content.MinimumSize.Width + padding.Horizontal, content.MinimumSize.Height + padding.Vertical);

         MinimumSize = contentMinimumSize;
         MaximumSize = contentSize;
         Size = contentSize;

         ToolStripControlHost host = new System.Windows.Forms.ToolStripControlHost(content);
         Items.Add(host);
      }
   }
}

