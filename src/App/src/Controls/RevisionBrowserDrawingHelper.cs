using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Aga.Controls.Tree;
using Aga.Controls.Tree.NodeControls;
using mrHelper.App.Helpers;

namespace mrHelper.App.Controls
{
   internal static class RevisionBrowserDrawingHelper
   {
      internal static void DrawRow(TreeViewAdv treeView, TreeViewRowDrawEventArgs e)
      {
         if (e.Node.IsSelected)
         {
            Rectangle focusRect = new Rectangle(
               treeView.OffsetX, e.RowRect.Y, treeView.ClientRectangle.Width, e.RowRect.Height);
            using (Brush brush = new SolidBrush(ThemeSupport.StockColors.GetThemeColors().SelectionBackground))
            {
               e.Graphics.FillRectangle(brush, focusRect);
            }
         }
      }

      internal static void DrawGridLine(TreeViewGridLineDrawEventArgs e)
      {
         using (Pen p = new Pen(ThemeSupport.StockColors.GetThemeColors().ListViewColumnHeaderBackground))
         {
            e.Graphics.DrawLine(p, e.Rect.Left, e.Rect.Top, e.Rect.Right, e.Rect.Bottom);
            e.Handled = true;
         }
      }

      internal static void DrawColumnHeaderBackground(DrawColHeaderBgEventArgs args)
      {
         Color backgroundColor = ThemeSupport.StockColors.GetThemeColors().ListViewColumnHeaderBackground;
         using (Brush brush = new SolidBrush(backgroundColor))
         {
            args.Graphics.FillRectangle(brush, args.Bounds);
         }
         args.Handled = true;
      }

      internal static void DrawColumnHeaderText(TreeViewAdv treeView, DrawColHeaderTextEventArgs args)
      {
         TextRenderer.DrawText(args.Graphics, args.Text, treeView.Font, args.Bounds,
            ThemeSupport.StockColors.GetThemeColors().ListViewColumnHeaderTextColor, args.Flags);
         args.Handled = true;
      }

      internal static void DrawNode(DrawTextEventArgs args)
      {
         if (args.Node.IsSelected)
         {
            args.TextColor = ThemeSupport.StockColors.GetThemeColors().OSThemeColors.TextInSelection;
         }
         else
         {
            args.TextColor = ThemeSupport.StockColors.GetThemeColors().OSThemeColors.TextActive;
         }
      }

      internal static void ApplyFont(TreeViewAdv treeView, Font font)
      {
         string preferredFontFamily = "Segoe UI";
         bool preferredFontFamilySupported = FontFamily.Families.Any(family => family.Name == preferredFontFamily);
         string selectedFamily = preferredFontFamilySupported ? preferredFontFamily : font.FontFamily.Name;
         treeView.Font = new Font(selectedFamily, font.Size, font.Style,
            GraphicsUnit.Point, font.GdiCharSet, font.GdiVerticalFont);
      }
   }
}
