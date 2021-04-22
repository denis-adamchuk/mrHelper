using mrHelper.CommonControls.Tools;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrHelper.App.Helpers
{
   public class IconCache
   {
      internal bool ContainsKey(Color color)
      {
         return _iconCache.ContainsKey(color);
      }

      internal void Add(Color color, IconGroup iconGroup)
      {
         _iconCache.Add(color, iconGroup);
      }

      internal Icon Get(Color color)
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

      private readonly Dictionary<Color, IconGroup> _iconCache = new Dictionary<Color, IconGroup>();
   }
}
