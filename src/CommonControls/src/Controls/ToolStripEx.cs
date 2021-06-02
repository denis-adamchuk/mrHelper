using mrHelper.CommonNative;
using System;
using System.Windows.Forms;

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
}

