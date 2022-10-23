using System;
using System.Drawing;
using System.Windows.Forms;
using mrHelper.CommonNative;

namespace mrHelper.CommonControls.Controls
{
   // from https://stackoverflow.com/a/1892990
   internal static class CommonActivationHandler
   {
      internal static void Handle(ref Message m, bool clickThrough)
      {
         if (clickThrough
          && m.Msg == NativeMethods.WM_MOUSEACTIVATE
          && m.Result == (IntPtr)NativeMethods.MA_ACTIVATEANDEAT)
         {
            m.Result = (IntPtr)NativeMethods.MA_ACTIVATE;
         }
      }
   }

   public class ToolStripEx : ToolStrip
   {
      public bool ClickThrough { get; set; }

      protected override void WndProc(ref Message m)
      {
         base.WndProc(ref m);
         CommonActivationHandler.Handle(ref m, ClickThrough);
      }
   }

   public class MenuStripEx : MenuStrip
   {
      public bool ClickThrough { get; set; }

      protected override void WndProc(ref Message m)
      {
         base.WndProc(ref m);
         CommonActivationHandler.Handle(ref m, ClickThrough);
      }
   }

   public class StatusStripEx : StatusStrip
   {
      public bool ClickThrough { get; set; }

      protected override void WndProc(ref Message m)
      {
         base.WndProc(ref m);
         CommonActivationHandler.Handle(ref m, ClickThrough);
      }
   }

   // https://social.msdn.microsoft.com/Forums/en-US/c48fc24e-9bd6-4879-a992-507b8e008b52/blinking-tooltip-of-toolstripstatuslabel?forum=winforms
   public class ToolStripStatusLabelEx : ToolStripStatusLabel
   {
      public void SetTooltip(ToolTip tooltip)
      {
         _toolTip = tooltip;
      }

      protected override void OnMouseHover(System.EventArgs e)
      {
         Point loc = new Point(Control.MousePosition.X + 30, Control.MousePosition.Y);
         loc = this.Parent.PointToClient(loc);
         _toolTip?.Show(this.ToolTipText, this.Parent, loc);
      }

      protected override void OnMouseLeave(System.EventArgs e)
      {
         _toolTip?.Hide(this.Parent);
         base.OnMouseLeave(e);
      }

      private ToolTip _toolTip;
   }
}

