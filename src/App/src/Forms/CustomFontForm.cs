using System;
using System.Drawing;
using System.Windows.Forms;
using mrHelper.Common;
using mrHelper.Common.Constants;

namespace mrHelper.App.Forms
{
   public partial class CustomFontForm : Form
   {
      protected void applyFont(string font)
      {
         if (!Constants.FontSizeChoices.ContainsKey(font))
         {
            return;
         }

         this.Font = new Font(this.Font.FontFamily, (float)Constants.FontSizeChoices[font],
            this.Font.Style, GraphicsUnit.Point, this.Font.GdiCharSet, this.Font.GdiVerticalFont);
      }
   }
}

