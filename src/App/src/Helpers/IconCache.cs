using System.Collections.Generic;
using System.Drawing;
using mrHelper.CommonControls.Tools;

namespace mrHelper.App.Helpers
{
   internal static class IconCache
   {
      internal static Icon Get(Color color)
      {
         if (!_iconCache.TryGetValue(color, out IconGroup icon))
         {
            icon = addIconToCache(color);
         }
         bool useBorder = WinFormsHelpers.IsLightThemeUsed();
         return useBorder ? icon.IconWithBorder : icon.IconWithoutBorder;
      }

      private struct IconGroup
      {
         internal IconGroup(Icon iconWithoutBorder, Icon iconWithBorder)
         {
            IconWithoutBorder = iconWithoutBorder;
            IconWithBorder = iconWithBorder;
         }

         internal Icon IconWithoutBorder { get; }
         internal Icon IconWithBorder { get; }
      }

      private static IconGroup addIconToCache(Color color)
      {
         Bitmap imageWithoutBorder = WinFormsHelpers.ReplaceColorInBitmap(
            Properties.Resources.gitlab_icon_stub_16x16, Color.Green, color);
         Icon iconWithoutBorder = WinFormsHelpers.ConvertToIco(imageWithoutBorder, 16);

         Bitmap imageWithBorder = WinFormsHelpers.ReplaceColorInBitmap(
            Properties.Resources.gitlab_icon_stub_16x16_border, Color.Green, color);
         Icon iconWithBorder = WinFormsHelpers.ConvertToIco(imageWithBorder, 16);

         IconGroup icon = new IconGroup(iconWithoutBorder, iconWithBorder);
         _iconCache[color] = icon;
         return icon;
      }

      private static readonly Dictionary<Color, IconGroup> _iconCache = new Dictionary<Color, IconGroup>();
   }
}

