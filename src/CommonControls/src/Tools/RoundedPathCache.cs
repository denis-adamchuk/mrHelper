using System;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;

namespace mrHelper.CommonControls.Tools
{
   public class RoundedPathCache : IDisposable
   {
      public RoundedPathCache(int radius)
      {
         _radius = radius;
      }

      public GraphicsPath GetPath(Rectangle bounds, bool isHorizontalScrollVisible)
      {
         GraphicsPath pathToUse;
         Dictionary<Rectangle, GraphicsPath> cache =
            isHorizontalScrollVisible ? _pathWithScrollBarCache : _pathWithoutScrollBarCache;
         if (cache.TryGetValue(bounds, out GraphicsPath path))
         {
            pathToUse = path;
         }
         else
         {
            pathToUse = CommonControls.Tools.WinFormsHelpers.GetRoundedPath(
               bounds, _radius, isHorizontalScrollVisible);
            cache[bounds] = pathToUse;
         }
         return pathToUse;
      }

      public void Dispose()
      {
         _pathWithoutScrollBarCache.Concat(_pathWithScrollBarCache)
            .Select(kv => kv.Value)
            .ToList()
            .ForEach(p => p.Dispose());
      }

      private readonly int _radius;

      private Dictionary<Rectangle, GraphicsPath> _pathWithScrollBarCache =
         new Dictionary<Rectangle, GraphicsPath>();
      private Dictionary<Rectangle, GraphicsPath> _pathWithoutScrollBarCache =
         new Dictionary<Rectangle, GraphicsPath>();
   }
}

