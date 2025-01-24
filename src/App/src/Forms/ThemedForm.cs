using System;
using System.Diagnostics;
using mrHelper.Common.Constants;
using mrHelper.App.Helpers;

namespace mrHelper.App.Forms
{
   internal partial class ThemedForm : CustomFontForm
   {
      internal ThemedForm()
      {
         if (Program.Settings != null)
         {
            Program.Settings.ColorModeChanged += onColorModeChanged;
         }
      }

      protected override void Dispose(bool disposing)
      {
         if (Program.Settings != null)
         {
            Program.Settings.ColorModeChanged -= onColorModeChanged;
         }
         base.Dispose(disposing);
      }

      protected override void OnHandleCreated(EventArgs e)
      {
         base.OnHandleCreated(e);

         if (Program.Settings == null)
         {
            return;
         }

         Constants.ColorMode colorMode = ConfigurationHelper.GetColorMode(Program.Settings);

         // ThemeSupportHelper adds OnLoad() handler to hack all the controls inside it
         if (colorMode == Constants.ColorMode.Dark)
         {
            _themeSupportHelper = new ThemeSupport.ThemeSupportHelper(this, ThemeSupport.DisplayMode.DarkMode);
         }
         else
         {
            Debug.Assert(colorMode == Constants.ColorMode.Light);
            _themeSupportHelper = new ThemeSupport.ThemeSupportHelper(this, ThemeSupport.DisplayMode.ClearMode);
         }
      }

      private void onColorModeChanged()
      {
         _themeSupportHelper?.ApplyThemeFromConfiguration();
      }
	  
      private ThemeSupport.ThemeSupportHelper _themeSupportHelper = null;
   }
}

