using System.Drawing;
using System.Windows.Forms;
using TheArtOfDev.HtmlRenderer.WinForms;
using mrHelper.CommonControls.Tools;

namespace mrHelper.App.Controls
{
   public class HtmlPanelEx : HtmlPanel
   {
      public HtmlPanelEx(CommonControls.Tools.RoundedPathCache pathCache, bool needShowBorder)
      {
         _pathCache = pathCache;
         NeedShowBorder = needShowBorder;
      }

      public bool NeedShowBorder { get; }

      public bool ShowBorder { get; set; }

      public void UpdateRegion()
      {
         updateRegion(this);
      }

      protected override void OnPaint(PaintEventArgs e)
      {
         base.OnPaint(e);
         if (NeedShowBorder)
         {
            if (Focused)
            {
               drawBorders(this, e.Graphics, Color.Black);
            }
            else if (ShowBorder)
            {
               drawBorders(this, e.Graphics, Color.Gray);
            }
         }
      }

      protected override void OnScroll(ScrollEventArgs se)
      {
         if (NeedShowBorder)
         {
            Invalidate();
         }
         base.OnScroll(se);
      }

      private void updateRegion(Control control)
      {
         Rectangle bounds = control.ClientRectangle;
         bool isHorizontalScrollVisible = (control as ScrollableControl)?.HorizontalScroll.Visible ?? false;
         control.Region = new Region(_pathCache.GetPath(bounds, isHorizontalScrollVisible));
      }

      private void drawBorders(Control control, Graphics graphics, Color borderColor)
      {
         using (Pen pen = new Pen(borderColor, 2.0f))
         {
            Rectangle bounds = control.ClientRectangle;
            bounds.X += 1;
            bounds.Y += 1;
            bounds.Width -= 2;
            bounds.Height -= 2;
            bool isHorizontalScrollVisible = (control as ScrollableControl)?.HorizontalScroll.Visible ?? false;
            graphics.DrawPath(pen, _pathCache.GetPath(bounds, isHorizontalScrollVisible));
         }
      }

      private readonly RoundedPathCache _pathCache;
   }
}

