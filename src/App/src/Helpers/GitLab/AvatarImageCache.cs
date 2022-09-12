using System;
using System.Collections.Generic;
using System.Drawing;
using GitLabSharp.Entities;
using mrHelper.CommonControls.Tools;
using mrHelper.GitLabClient;

namespace mrHelper.App.Helpers
{
   public class AvatarImageCache : IDisposable
   {
      public AvatarImageCache(DataCache dataCache)
      {
         _dataCache = dataCache;
         _dataCache.Connected += onDataCacheConnected;
      }

      public void Dispose()
      {
         if (_dataCache != null)
         {
            _dataCache.Connected -= onDataCacheConnected;
            _dataCache = null;
         }

         clearCache();
      }

      public Image GetAvatar(User user, Color backgroundColor)
      {
         if (user == null)
         {
            return null;
         }

         Key key = new Key(user.Id, backgroundColor);
         if (_cached.TryGetValue(key, out Image image))
         {
            return image;
         }

         image = convertByteToImage(_dataCache.AvatarCache.GetAvatar(user), backgroundColor);
         if (image == null)
         {
            return null;
         }

         _cached[key] = image;
         return image;
      }

      private Image convertByteToImage(byte[] bytes, Color backgroundColor)
      {
         if (bytes == null)
         {
            return null;
         }

         using (System.IO.MemoryStream ms = new System.IO.MemoryStream(bytes))
         {
            using (Image image = Image.FromStream(ms))
            {
               return WinFormsHelpers.ClipRectToCircle(image, backgroundColor);
            }
         }
      }

      private void onDataCacheConnected(string hostname, User user)
      {
         clearCache();
      }

      private void clearCache()
      {
         foreach (KeyValuePair<Key, Image> kv in _cached)
         {
            kv.Value.Dispose();
         }
         _cached.Clear();
      }

      private DataCache _dataCache;

      private struct Key
      {
         public Key(int userId, Color backgroundColo)
         {
            _userId = userId;
            _backgroundColo = backgroundColo;
         }

         int _userId;
         Color _backgroundColo;
      }
      private readonly Dictionary<Key, Image> _cached = new Dictionary<Key, Image>();
   }
}

