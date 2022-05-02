using System;
using System.Windows.Forms;

namespace mrHelper.CommonControls.Controls
{
   public class LinkLabelEx : LinkLabel
   {
      public LinkLabelEx()
         : base()
      {
         ContextMenuStrip = new ContextMenuStrip();
         ContextMenuStrip.Items.Add(new ToolStripMenuItem("Copy Link", null, onCopyLinkClicked));
      }

      public void SetLinkLabelClicked(Action<string> onLinkLabelClicked)
      {
         _onLinkLabelClicked = onLinkLabelClicked;
      }

      private void onCopyLinkClicked(object sender, EventArgs e)
      {
         if (!String.IsNullOrWhiteSpace(Text))
         {
            Clipboard.SetText(Text);
         }
      }

      protected override void Dispose(bool disposing)
      {
         base.Dispose(disposing);
         ContextMenuStrip.Dispose();
         ContextMenuStrip = null;
         _onLinkLabelClicked = null;
      }

      protected override void OnLinkClicked(LinkLabelLinkClickedEventArgs e)
      {
         if (e.Button == MouseButtons.Left)
         {
            _onLinkLabelClicked?.Invoke(Text);
         }
         base.OnLinkClicked(e);
      }

      private Action<string> _onLinkLabelClicked;
   }
}

