using System;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using TheArtOfDev.HtmlRenderer.WinForms;
using mrHelper.CommonControls.Tools;
using mrHelper.App.Helpers;

namespace mrHelper.App.Controls
{
   public class HtmlPanelEx : HtmlPanel
   {
      public HtmlPanelEx(CommonControls.Tools.RoundedPathCache pathCache,
         bool isBorderSupported, bool scrollOnMouseWheel)
      {
         _pathCache = pathCache;
         IsBorderSupported = isBorderSupported;
         ScrollOnMouseWheel = scrollOnMouseWheel;
         Debug.Assert(!IsBorderSupported || _pathCache != null);
      }

      public bool IsBorderSupported { get; }

      public bool ScrollOnMouseWheel { get; }

      public bool ShowBorderWhenNotFocused { get; set; }

      public void FlickBorder()
      {
         if (_flickeringColors == null)
         {
            _flickeringColors = new Color[8];
         }

         Color flickering1 = ThemeSupport.StockColors.GetThemeColors().OSThemeColors.TextActive;
         Color flickering2 = ThemeSupport.StockColors.GetThemeColors().OSThemeColors.Control;

         Debug.Assert(_flickeringColors.Length % 2 == 0);
         for (int i = 0; i < _flickeringColors.Length - 1; i += 2)
         {
            _flickeringColors[i] = flickering1;
            _flickeringColors[i + 1] = flickering2;
         }

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
         _borderColor = ThemeSupport.StockColors.GetThemeColors().OSThemeColors.TextActive;
         base.OnGotFocus(e);
         Invalidate();
      }

      protected override void OnLostFocus(EventArgs e)
      {
         _borderColor = ThemeSupport.StockColors.GetThemeColors().OSThemeColors.TextInactive;
         base.OnLostFocus(e);
         Invalidate();
      }

      protected override void OnPaint(PaintEventArgs e)
      {
         base.OnPaint(e);
         if (needShowBorder())
         {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
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

      public struct MouseWheelExArgs
      {
         public MouseWheelExArgs(int delta) => Delta = delta;

         public int Delta { get; }
      }
      public delegate void MouseWheelExHandler(object sender, MouseWheelExArgs e);
      public event MouseWheelExHandler MouseWheelEx;

      protected override void WndProc(ref Message m)
      {
         if (m.Msg == CommonNative.NativeMethods.WM_MOUSEWHEEL && !ScrollOnMouseWheel)
         {
            int delta = CommonNative.NativeMethods.GET_WHEEL_DELTA_WPARAM(m.WParam);
            MouseWheelEx?.Invoke(this, new MouseWheelExArgs(delta));
            return;
         }
         base.WndProc(ref m);
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
         Color borderColor = _flickeringBorderColor ?? _borderColor;
         using (Pen pen = new Pen(borderColor, 2.0f))
         {
            Rectangle bounds = control.ClientRectangle;
            bounds.X += 1;
            bounds.Y += 1;
            bounds.Width -= 3;
            bounds.Height -= 3;
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

      private Color[] _flickeringColors;
      private static readonly int FlickeringTimerIntervalMs = 300;
      private Timer _timer;

      private readonly RoundedPathCache _pathCache;

      private Color _borderColor = Color.Transparent;
      private Color? _flickeringBorderColor = new Color?();
   }
}

