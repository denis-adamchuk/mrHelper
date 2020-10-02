using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using mrHelper.CommonNative;

namespace mrHelper.CommonControls.Controls
{
   /// <summary>
   /// Two things differ this class from its parent:
   /// - It does not stuck on mouse wheel
   /// - It calculates full preferred height which is not possible to get from TextBox in multiline mode
   /// </summary>
   public class TextBoxEx : TextBox
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

      public int FullPreferredHeight
      {
         get
         {
            int numberOfNewLines = Text.Count(ch => ch == '\r');
            return calcPreferredHeight(numberOfNewLines + 1);
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
               _cachedBorderHeight = Math.Max(0, borderHeight);
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

