using System.Drawing;
using System.Windows.Forms;
using mrHelper.App.Helpers;

namespace mrHelper.App.Controls
{
   internal class ThemedToolTip : ToolTip
   {
      internal ThemedToolTip()
      {
         Draw += onDraw;
         OwnerDraw = true;
      }

      internal ThemedToolTip(System.ComponentModel.IContainer cont)
         : base(cont)
      {
         Draw += onDraw;
         OwnerDraw = true;
      }

      protected override void Dispose(bool disposing)
      {
         Draw -= onDraw;
         base.Dispose(disposing);
      }

      private void onDraw(object sender, DrawToolTipEventArgs e)
      {
         if (System.String.IsNullOrEmpty(e.ToolTipText))
         {
            return; // Empty string happens here for unknown reason sometimes.
                    // If we draw it, then good text is not drawn.
                    // If we ignore it, a next call will carry a good text.
         }

         Color backColor = ThemeSupport.StockColors.GetThemeColors().TooltipBackground;
         Color foreColor = ThemeSupport.StockColors.GetThemeColors().OSThemeColors.TextActive;

         using (System.Drawing.Brush brush = new System.Drawing.SolidBrush(backColor))
         {
            e.Graphics.FillRectangle(brush, e.Bounds);
         }

         e.DrawBorder();

         TextRenderer.DrawText(e.Graphics, e.ToolTipText, e.Font, e.Bounds, foreColor, 
            TextFormatFlags.HidePrefix | TextFormatFlags.VerticalCenter);
      }
   }
}
