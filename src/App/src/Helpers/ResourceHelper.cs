using System;
using System.Windows.Forms;
using mrHelper.CommonControls.Tools;

namespace mrHelper.App.Helpers
{
   internal static class ResourceHelper
   {
      internal static string SetControlFontSizeToCommonCss(Control control)
      {
         string css = Properties.Resources.Common_CSS;
         string textColor = DarkModeForms.DarkModeCS.GetSystemColors().TextActive.Name;
         return String.Format("{0} body {{ font-size: {1}pt; color: {2}; }}", css,
            WinFormsHelpers.GetFontSizeInPoints(control), textColor);
      }
   }
}

