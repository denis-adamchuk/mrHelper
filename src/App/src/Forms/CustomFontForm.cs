using System;
using System.Drawing;
using System.Windows.Forms;
using mrHelper.Common;
using mrHelper.Common.Constants;

namespace mrHelper.App.Forms
{
   public partial class CustomFontForm : Form
   {
      protected CustomFontForm()
      {
         _originalFontSize = this.Font.Size;
      }

      private float _originalFontSize;

      protected void applyFont(string font)
      {
         if (!Constants.FontSizeChoices.ContainsKey(font))
         {
            return;
         }

         this.Font = new Font(this.Font.FontFamily, (float)Constants.FontSizeChoices[font]);
      }

      public float CurrentFontMultiplier
      {
         get { return this.Font.Size / _originalFontSize; }
      }
   }
}

