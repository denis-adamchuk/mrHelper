using System;
using System.Collections.Generic;
using System.Diagnostics;
using TheArtOfDev.HtmlRenderer.WinForms;

namespace mrHelper.App.Helpers
{
   public static class DiffContextHelpers
   {
      internal struct EstimateWidthKey : IEquatable<EstimateWidthKey>
      {
         public double FontSizePx;
         public string HtmlSnippet;
         public int ActualWidth;

         public bool Equals(EstimateWidthKey other)
         {
            return FontSizePx == other.FontSizePx && //-V3024
                   HtmlSnippet == other.HtmlSnippet &&
                   ActualWidth == other.ActualWidth;
         }

         public override int GetHashCode()
         {
            int hashCode = -894020761;
            hashCode = hashCode * -1521134295 + FontSizePx.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(HtmlSnippet);
            hashCode = hashCode * -1521134295 + ActualWidth.GetHashCode();
            return hashCode;
         }
      }

      internal class EstimateWidthCache : Dictionary<EstimateWidthKey, int> { }

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

