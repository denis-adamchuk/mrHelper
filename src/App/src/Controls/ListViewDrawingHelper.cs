using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using mrHelper.CommonControls.Tools;

namespace mrHelper.App.Controls
{
   internal static class ListViewDrawingHelper
   {
      internal static void DrawGroupHeader(int groupHeaderHeight, DrawListViewSubItemEventArgs e)
      {
         Debug.Assert(e.Item.ListView.HeaderStyle != System.Windows.Forms.ColumnHeaderStyle.None);

         int headerHeight = groupHeaderHeight;
         ListViewHitTestInfo testAboveMe = e.Item.ListView.HitTest(0, e.Bounds.Location.Y - headerHeight);
         if (e.Item.Group != null && (testAboveMe.Item == null || testAboveMe.Item.Group != e.Item.Group))
         {
            Rectangle headerBounds = e.Item.Bounds;
            headerHeight += 1; // Group header starts 1 pixel above the e.Item.Bounds.Y - headerHeight
            headerBounds.Y -= headerHeight;
            headerBounds.Height = headerHeight;
            Color groupHeaderColor = ThemeSupport.StockColors.GetThemeColors().ListViewGroupHeaderBackground;
            WinFormsHelpers.FillRectangle(e, headerBounds, groupHeaderColor);

            Color textColor = ThemeSupport.StockColors.GetThemeColors().ListViewGroupHeaderTextColor;
            using (Brush textBrush = new SolidBrush(textColor))
            {
               headerBounds.X = -CommonNative.Win32Tools.GetHorizontalScrollPosition(e.Item.ListView.Handle);
               using (StringFormat format = new StringFormat(StringFormatFlags.NoClip | StringFormatFlags.NoWrap))
               {
                  format.Trimming = StringTrimming.None;
                  format.LineAlignment = StringAlignment.Center;
                  e.Graphics.DrawString(e.Item.Group.Header, e.Item.ListView.Font, textBrush, headerBounds, format);
               }
            }
         }
      }

      internal static void DrawColumnHeaderBackground(DrawListViewColumnHeaderEventArgs e)
      {
         Color gridColor = ThemeSupport.StockColors.GetThemeColors().ListViewColumnHeaderGridColor;
         using (Brush brush = new SolidBrush(gridColor))
         {
            // Fill rectangle with dark color
            e.Graphics.FillRectangle(brush, e.Bounds);
         }

         Color backgroundColor = ThemeSupport.StockColors.GetThemeColors().ListViewColumnHeaderBackground;
         using (Brush brush = new SolidBrush(backgroundColor))
         {
            // Fill rectangle with lighter color but leave 1px for a "border".
            // 1px is enough no matter which DPI is used.
            Rectangle rect = new Rectangle(e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1 /* px */, e.Bounds.Height);
            e.Graphics.FillRectangle(brush, rect);
         }
      }

      internal static void DrawColumnHeaderText(DrawListViewColumnHeaderEventArgs e, Font font)
      {
         Color textColor = ThemeSupport.StockColors.GetThemeColors().ListViewColumnHeaderTextColor;
         using (Brush brush = new SolidBrush(textColor))
         {
            StringFormat format = new StringFormat(StringFormatFlags.NoWrap)
            {
               Trimming = StringTrimming.EllipsisCharacter
            };
            e.Graphics.DrawString(e.Header.Text, font, brush, e.Bounds, format);
         }
      }
   }
}
