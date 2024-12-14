using System;
using System.Windows.Forms;
using mrHelper.Common.Tools;
using mrHelper.CommonControls.Tools;

namespace mrHelper.App.Helpers
{
   internal static class ResourceHelper
   {
      internal static string ApplyFontSizeAndColorsToCSS(Control control)
      {
         return String.Format(
            @"
            {0}
            body {{
               font-size: {1}pt;
               color: {2};
            }}
            table {{
               border: solid 1px {3};
            }}
            table thead th {{
               color: {4};
               text-shadow: 1px 1px 1px {5};
               background-color: {6};
               border: solid 1px {3};
            }}
            table tbody td {{
               color: {7};
               text-shadow: 1px 1px 1px {8};
               background-color: {9};
               border: solid 1px {3};
            }}
            .highlight {{
                background-color: {10};
            }}
            a:link {{
               color: {11};
            }}",
            Properties.Resources.Common_CSS,
            WinFormsHelpers.GetFontSizeInPoints(control),
            HtmlUtils.ColorToRgb(ThemeSupport.StockColors.GetThemeColors().OSThemeColors.TextActive),
            HtmlUtils.ColorToRgb(ColorScheme.GetColor("HTML_Table_Border").Color),
            HtmlUtils.ColorToRgb(ColorScheme.GetColor("HTML_Table_Text").Color),
            HtmlUtils.ColorToRgb(ColorScheme.GetColor("HTML_Table_Text_Shadow").Color),
            HtmlUtils.ColorToRgb(ColorScheme.GetColor("HTML_Table_Background").Color),
            HtmlUtils.ColorToRgb(ColorScheme.GetColor("HTML_Table_Text").Color),
            HtmlUtils.ColorToRgb(ColorScheme.GetColor("HTML_Table_Text_Shadow").Color),
            HtmlUtils.ColorToRgb(ColorScheme.GetColor("HTML_Table_Background").Color),
            HtmlUtils.ColorToRgb(ColorScheme.GetColor("HTML_Highlight_Background").Color),
            HtmlUtils.ColorToRgb(ThemeSupport.StockColors.GetThemeColors().LinkTextColor));
      }
   }
}

