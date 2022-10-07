using System;
using System.Diagnostics;
using System.Linq;
using mrHelper.Core.Context;
using TheArtOfDev.HtmlRenderer.WinForms;

namespace mrHelper.App.Helpers
{
   public static class DiffContextHelpers
   {
      public static int EstimateHtmlWidth(DiffContext context, double fontSizePx, int minWidth)
      {
         Debug.Assert(minWidth >= 0);

         string longestLine = context.Lines
            .Select(line => line.Text)
            .OrderBy(line => line.Length)
            .LastOrDefault();
         if (longestLine != null)
         {
            string html = DiffContextFormatter.GetHtml(longestLine, fontSizePx, 0, null);
            _htmlPanelForWidthCalculation.Width = minWidth;
            _htmlPanelForWidthCalculation.Text = html;
            while (true)
            {
               if (_htmlPanelForWidthCalculation.AutoScrollMinSize.Height <= fontSizePx * 1.25
                || _htmlPanelForWidthCalculation.Width >= 9999) // safety limit
               {
                  return Math.Max(_htmlPanelForWidthCalculation.AutoScrollMinSize.Width, minWidth);
               }
               _htmlPanelForWidthCalculation.Width = Convert.ToInt32(_htmlPanelForWidthCalculation.Width * 1.1);
            }
         }
         return minWidth;
      }

      // Yes, this static member is not be disposed but this decision reduces overall design complexity
      private static HtmlPanel _htmlPanelForWidthCalculation = new HtmlPanel { Width = 1 };
   }
}

