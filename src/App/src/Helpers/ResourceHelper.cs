using System;
using System.Windows.Forms;
using mrHelper.CommonControls.Tools;

namespace mrHelper.App.Helpers
{
   internal static class ResourceHelper
   {
      internal static string SetControlFontSizeToCommonCss(Control control) =>
         String.Format("{0} body div {{ font-size: {1}pt; }}",
            Properties.Resources.Common_CSS, WinFormsHelpers.GetFontSizeInPoints(control));
   }
}

