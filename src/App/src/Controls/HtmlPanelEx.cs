using System;
using System.Drawing;
using System.Windows.Forms;
using TheArtOfDev.HtmlRenderer.WinForms;
using mrHelper.CommonControls.Tools;

namespace mrHelper.App.Controls
{
   public class HtmlPanelEx : HtmlPanel
   {
      public HtmlPanelEx(CommonControls.Tools.RoundedPathCache pathCache, bool isBorderSupported)
      {
         _pathCache = pathCache;
         IsBorderSupported = isBorderSupported;
      }

      public bool IsBorderSupported { get; }

      public bool ShowBorderWhenNotFocused { get; set; }

      public void FlickBorder()
      {
         startFlickering(FlickeringTimerIntervalMs);
      }

      public void UpdateRegion()
      {
         updateRegion(this);
      }

      protected override void Dispose(bool disposing)
      {
         stopTimer();
      }

      protected override void OnGotFocus(EventArgs e)
      {
         _borderColor = FocusedBorderColor;
         base.OnGotFocus(e);
      }

      protected override void OnLostFocus(EventArgs e)
      {
         _borderColor = NotFocusedBorderColor;
         base.OnLostFocus(e);
      }

      protected override void OnPaint(PaintEventArgs e)
      {
         base.OnPaint(e);
         if (needShowBorder())
         {
            drawBorders(this, e.Graphics);
         }
      }

      protected override void OnMouseWheel(MouseEventArgs e)
      {
         if (IsBorderSupported)
         {
            Invalidate(); // invalidate the entire surface of the control in order to redraw borders
         }
         base.OnMouseWheel(e);
      }

      protected override void OnScroll(ScrollEventArgs se)
      {
         if (IsBorderSupported)
         {
            Invalidate(); // invalidate the entire surface of the control in order to redraw borders
         }
         base.OnScroll(se);
      }

      private bool needShowBorder()
      {
         return IsBorderSupported && (Focused || ShowBorderWhenNotFocused);
      }

      private void startFlickering(int intervalMs)
      {
         changeBorderColorDelayed(0, intervalMs);
      }

      private void changeBorderColorDelayed(int flickeringColorIndex, int intervalMs)
      {
         if (needShowBorder())
         {
            startTimer(intervalMs, () =>
            {
               if (needShowBorder())
               {
                  if (flickeringColorIndex < _flickeringColors.Length)
                  {
                     _flickeringBorderColor = _flickeringColors[flickeringColorIndex];
                     Invalidate();
                     changeBorderColorDelayed(flickeringColorIndex + 1, intervalMs);
                     return;
                  }
               }
               _flickeringBorderColor = null;
               Invalidate();
            });
         }
      }

      private void updateRegion(Control control)
      {
         Rectangle bounds = control.ClientRectangle;
         bool isHorizontalScrollVisible = (control as ScrollableControl)?.HorizontalScroll.Visible ?? false;
         control.Region = new Region(_pathCache.GetPath(bounds, isHorizontalScrollVisible));
      }

      private void drawBorders(Control control, Graphics graphics)
      {
         Color borderColor = _flickeringBorderColor.HasValue ? _flickeringBorderColor.Value : _borderColor;
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

      private void startTimer(int interval, Action onTimer)
      {
         stopTimer();

         _timer = new Timer();
         _timer.Tick += (s, e) =>
         {
            stopTimer();
            onTimer?.Invoke();
         };
         _timer.Interval = interval;
         _timer.Start();
      }

      private void stopTimer()
      {
         _timer?.Stop();
         _timer?.Dispose();
         _timer = null;
      }

      private static readonly Color FocusedBorderColor = Color.Black;
      private static readonly Color NotFocusedBorderColor = Color.Gray;

      private static readonly Color[] _flickeringColors = new Color[] { Color.White, Color.Black, Color.White, Color.Black, Color.White };
      private static readonly int FlickeringTimerIntervalMs = 300;
      private Timer _timer;

      private readonly RoundedPathCache _pathCache;

      private Color _borderColor = Color.Transparent;
      private Color? _flickeringBorderColor = new Color?();
   }
}

