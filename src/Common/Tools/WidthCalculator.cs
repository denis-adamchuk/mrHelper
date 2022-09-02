using System;
using System.Drawing;
using System.Windows.Forms;

namespace mrHelper.Common.Tools
{
   public class WidthCalculator
   {
      public WidthCalculator(Control c)
      {
         _c = c;
      }

      public int CalculateWidth(string text)
      {
         int fontSizeInPoints = 10; // found experimentally for TheArtOfDev.HtmlPanel which is used across the app
         using (Graphics g = _c.CreateGraphics())
         {
            FontFamily fontFamily = new FontFamily(System.Drawing.Text.GenericFontFamilies.Monospace);
            using (Font f = new Font(fontFamily, fontSizeInPoints))
            {
               return Convert.ToInt32(Math.Ceiling(g.MeasureString(text, f).Width));
            }
         }
      }

      private readonly Control _c;
   }
}

