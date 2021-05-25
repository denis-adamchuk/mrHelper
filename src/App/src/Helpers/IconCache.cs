using System.Collections.Generic;
using System.Drawing;
using mrHelper.CommonControls.Tools;

namespace mrHelper.App.Helpers
{
   internal static class IconCache
   {
      internal static bool ContainsKey(Color color)
      {
         return _iconCache.ContainsKey(color);
      }

      internal static void Add(Color color, IconGroup iconGroup)
      {
         _iconCache.Add(color, iconGroup);
      }

      internal static Icon Get(Color color)
      {
         if (_iconCache.TryGetValue(color, out IconGroup icon))
         {
            bool useBorder = WinFormsHelpers.IsLightThemeUsed();
            return useBorder ? icon.IconWithBorder : icon.IconWithoutBorder;
         }
         return null;
      }

      internal struct IconGroup
      {
         internal IconGroup(Icon iconWithoutBorder, Icon iconWithBorder)
         {
            IconWithoutBorder = iconWithoutBorder;
            IconWithBorder = iconWithBorder;
         }

         internal Icon IconWithoutBorder { get; }
         internal Icon IconWithBorder { get; }
      }

      private static readonly Dictionary<Color, IconGroup> _iconCache = new Dictionary<Color, IconGroup>();
   }
}

