using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using TheArtOfDev.HtmlRenderer.WinForms;

namespace mrHelper.App.Controls
{
   public class HtmlToolTipEx : HtmlToolTip
   {
      public new void SetToolTip(Control control, string text)
      {
         if (String.IsNullOrEmpty(text))
         {
            if (_userText.ContainsKey(control))
            {
               _userText.Remove(control);
            }
            base.SetToolTip(control, null);
         }
         else
         {
            _userText[control] = text;
            base.SetToolTip(control, DefaultToolTipText);
         }
      }

      [DllImport("user32.dll")]
      public static extern IntPtr WindowFromDC(IntPtr hdc);

      [DllImport("User32.dll")]
      public static extern bool MoveWindow(IntPtr handle, int x, int y, int width, int height, bool redraw);

      // copied from HtmlToolTip.OnToolTipPopup
      protected override void OnToolTipPopup(PopupEventArgs e)
      {
         string cssClass = String.IsNullOrEmpty(base.TooltipCssClass)
            ? null
            : String.Format(" class=\"{0}\"", base.TooltipCssClass);
         string toolipHtml = string.Format("<div{0}>{1}</div>", cssClass, getToolTip(e.AssociatedControl));
         _htmlContainer.SetHtml(toolipHtml, _baseCssData);
         _htmlContainer.MaxSize = MaximumSize;

         //Measure size of the container
         using (Graphics g = e.AssociatedControl.CreateGraphics())
         {
            g.TextRenderingHint = _textRenderingHint;
            _htmlContainer.PerformLayout(g);
         }

         //Set the size of the tooltip
         int desiredWidth = (int)Math.Ceiling(MaximumSize.Width > 0
            ? Math.Min(_htmlContainer.ActualSize.Width, MaximumSize.Width) : _htmlContainer.ActualSize.Width);
         int desiredHeight = (int)Math.Ceiling(MaximumSize.Height > 0
            ? Math.Min(_htmlContainer.ActualSize.Height, MaximumSize.Height) : _htmlContainer.ActualSize.Height);
         e.ToolTipSize = new Size(desiredWidth, desiredHeight);
      }

      // copied from HtmlToolTip.OnToolTipDraw
      protected override void OnToolTipDraw(DrawToolTipEventArgs e)
      {
         if (_tooltipHandle == IntPtr.Zero)
         {
            // get the handle of the tooltip window using the graphics device context
            IntPtr hdc = e.Graphics.GetHdc();
            _tooltipHandle = WindowFromDC(hdc);
            e.Graphics.ReleaseHdc(hdc);
         }

         adjustTooltipPosition(e.AssociatedControl, e.Bounds.Size);

         e.Graphics.Clear(Color.White);
         e.Graphics.TextRenderingHint = _textRenderingHint;
         _htmlContainer.PerformPaint(e.Graphics);
      }

      // copied from HtmlToolTip.AdjustTooltipPosition
      private void adjustTooltipPosition(Control associatedControl, Size size)
      {
         var mousePos = Control.MousePosition;
         var screenBounds = Screen.FromControl(associatedControl).WorkingArea;

         // adjust if tooltip is outside form bounds
         if (mousePos.X + size.Width > screenBounds.Right)
         {
            mousePos.X = Math.Max(screenBounds.Right - size.Width - 5, screenBounds.Left + 3);
         }

         const int yOffset = 20;
         if (mousePos.Y + size.Height + yOffset > screenBounds.Bottom)
         {
            mousePos.Y = Math.Max(screenBounds.Bottom - size.Height - yOffset - 3, screenBounds.Top + 2);
         }

         // move the tooltip window to new location
         MoveWindow(_tooltipHandle, mousePos.X, mousePos.Y + yOffset, size.Width, size.Height, false);
      }

      private string getToolTip(Control control)
      {
         return _userText.TryGetValue(control, out string value) ? value : String.Empty;
      }

      private readonly static string DefaultToolTipText = "HtmlToolTipEx";
      private readonly Dictionary<Control, string> _userText = new Dictionary<Control, string>();
      private IntPtr _tooltipHandle = IntPtr.Zero;
   }
}

