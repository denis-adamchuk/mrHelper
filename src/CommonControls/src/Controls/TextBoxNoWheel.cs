using System;
using System.Diagnostics;
using System.Windows.Forms;
using mrHelper.CommonNative;

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

         _cachedBorderHeight = 0;
         _cachedSingleLineWithoutBorderHeight = 0;
      }

      protected override void OnBorderStyleChanged(EventArgs e)
      {
         base.OnBorderStyleChanged(e);

         _cachedHandle = IntPtr.Zero;
         _cachedBorderHeight = 0;
      }

      public new int PreferredHeight
      {
         get
         {
            Height = base.PreferredHeight; // without this, EM_GETLINECOUNT returns wrong result

            int numberOfLines = NativeMethods.SendMessage(CachedHandle, NativeMethods.EM_GETLINECOUNT,
               IntPtr.Zero, IntPtr.Zero).ToInt32();
            return calcPreferredHeight(numberOfLines);
         }
      }

      // TODO This is risky, because Handle may change when some TextBox property changes (e.g. BorderStyle).
      // It works in DiscussionBox but in general case should be revisited.
      private IntPtr CachedHandle
      {
         get
         {
            if (_cachedHandle == IntPtr.Zero)
            {
               _cachedHandle = Handle;
            }
            Debug.Assert(_cachedHandle == Handle);
            return _cachedHandle;
         }
      }

      private int calcPreferredHeight(int numberOfLines)
      {
         return SingleLineWithoutBorderHeight * numberOfLines + BorderHeight;
      }

      private int BorderHeight
      {
         get
         {
            if (_cachedBorderHeight == 0)
            {
               int singleLineWithBorderHeight = base.PreferredHeight;

               int borderHeight = singleLineWithBorderHeight - SingleLineWithoutBorderHeight;
               Debug.Assert(borderHeight >= 0);

               _cachedBorderHeight = borderHeight;
            }
            return _cachedBorderHeight;
         }
      }

      private int SingleLineWithoutBorderHeight
      {
         get
         {
            if (_cachedSingleLineWithoutBorderHeight == 0)
            {
               // textBox.FontHeight is too small, need to measure real letters
               _cachedSingleLineWithoutBorderHeight = TextRenderer.MeasureText(Alphabet, Font).Height;
            }
            return _cachedSingleLineWithoutBorderHeight;
         }
      }

      private IntPtr _cachedHandle;
      private int _cachedBorderHeight;
      private int _cachedSingleLineWithoutBorderHeight;
      private static readonly string Alphabet =
         "ABCDEFGHIJKLMONPQRSTUVWXYZabcdefghijklmonpqrstuvwxyz1234567890!@#$%^&*()";
   }
}

