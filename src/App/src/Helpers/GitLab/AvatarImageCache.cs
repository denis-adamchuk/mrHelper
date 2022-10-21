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
            return Properties.Resources.loading_transp_alpha;
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

      private struct Key : IEquatable<Key>
      {
         public Key(int userId, Color backgroundColo)
         {
            _userId = userId;
            _backgroundColor = backgroundColo;
         }

         private int _userId;
         private Color _backgroundColor;

         public override bool Equals(object obj)
         {
            return obj is Key key && Equals(key);
         }

         public bool Equals(Key other)
         {
            return _userId == other._userId &&
                   EqualityComparer<Color>.Default.Equals(_backgroundColor, other._backgroundColor);
         }

         public override int GetHashCode()
         {
            int hashCode = -1622434987;
            hashCode = hashCode * -1521134295 + _userId.GetHashCode();
            hashCode = hashCode * -1521134295 + _backgroundColor.GetHashCode();
            return hashCode;
         }
      }
      private readonly Dictionary<Key, Image> _cached = new Dictionary<Key, Image>();
   }
}

