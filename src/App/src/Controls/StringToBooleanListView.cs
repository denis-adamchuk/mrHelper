using System;
using System.Drawing;
using System.Windows.Forms;
using mrHelper.CommonControls.Tools;

namespace mrHelper.App.Controls
{
   internal class StringToBooleanListView : CommonControls.Controls.ListViewEx
   {
      public StringToBooleanListView()
         : base()
      {
      }

      protected override void OnDrawSubItem(DrawListViewSubItemEventArgs e)
      {
         if (e.Item.ListView == null)
         {
            return; // is being removed
         }

         Tuple<string, bool> tag = (Tuple<string, bool>)(e.Item.Tag);

         ListViewDrawingHelper.DrawGroupHeader(GroupHeaderHeight, e);

         bool isSelected = e.Item.Selected;
         if (isSelected)
         {
            Color color = ThemeSupport.StockColors.GetThemeColors().SelectionBackground;
            using (Brush brush = new SolidBrush(color))
            {
               e.Graphics.FillRectangle(brush, e.Bounds);
            }
         }

         Color textColor = tag.Item2
            ? ThemeSupport.StockColors.GetThemeColors().OSThemeColors.TextActive
            : ThemeSupport.StockColors.GetThemeColors().OSThemeColors.TextInactive;
         if (isSelected)
         {
            textColor = ThemeSupport.StockColors.GetThemeColors().OSThemeColors.TextInSelection;
         }

         using (Brush textBrush = new SolidBrush(textColor))
         {
            StringFormat format = new StringFormat
            {
               Trimming = StringTrimming.EllipsisCharacter,
               FormatFlags = StringFormatFlags.NoWrap
            };
            e.Graphics.DrawString(tag.Item1, e.Item.ListView.Font, textBrush, e.Bounds, format);
         }
      }

      private int scale(int px) => (int)WinFormsHelpers.ScalePixelsToNewDpi(96, DeviceDpi, px);

      private int GroupHeaderHeight => scale(20); // found experimentally
   }
}

