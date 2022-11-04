using System;
using System.Diagnostics;
using System.Linq;
using mrHelper.Core.Context;
using TheArtOfDev.HtmlRenderer.WinForms;

namespace mrHelper.App.Helpers
{
   public static class DiffContextHelpers
   {
      public static int EstimateHtmlWidth(string html, double fontSizePx, int minWidthPx)
      {
         Debug.Assert(minWidthPx >= 0);
         if (html != null)
         {
            _htmlPanelForWidthCalculation.Width = minWidthPx;
            _htmlPanelForWidthCalculation.Text = html;
            while (true)
            {
               if (_htmlPanelForWidthCalculation.AutoScrollMinSize.Height <= fontSizePx * 1.25
                || _htmlPanelForWidthCalculation.Width >= 9999) // safety limit
               {
                  return Math.Max(_htmlPanelForWidthCalculation.AutoScrollMinSize.Width, minWidthPx);
               }
               _htmlPanelForWidthCalculation.Width = Convert.ToInt32(_htmlPanelForWidthCalculation.Width * 1.1);
            }
         }
         return minWidthPx;
      }

      // Yes, this static member is not be disposed but this decision reduces overall design complexity
      private static HtmlPanel _htmlPanelForWidthCalculation = new HtmlPanel { Width = 1 };
   }
}

