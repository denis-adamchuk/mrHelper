using System.Diagnostics;
using System.Windows.Forms;

namespace mrHelper.CommonControls.Controls
{
   public class TextBoxNoWheel : TextBox
   {
      protected override void WndProc(ref Message m)
      {
         const int WM_MOUSEWHEEL = 0x020A;
         if (m.Msg == WM_MOUSEWHEEL)
         {
            m.HWnd = this.Parent.Handle;
         }
         base.WndProc(ref m);
      }

      protected override void OnFontChanged(System.EventArgs e)
      {
         base.OnFontChanged(e);

         _cachedFontHeight = 0;
      }

      public int CachedHandle
      {
         get
         {
            if (_cachedHandle == 0)
            {
               _cachedHandle = Handle.ToInt32();
            }
            Debug.Assert(_cachedHandle == Handle.ToInt32());
            return _cachedHandle;
         }
      }

      public new int FontHeight
      {
         get
         {
            if (_cachedFontHeight == 0)
            {
               _cachedFontHeight = Font.Height;
            }
            Debug.Assert(_cachedFontHeight == Font.Height);
            return _cachedFontHeight;
         }
      }

      private int _cachedHandle;
      private int _cachedFontHeight;
   }
}

