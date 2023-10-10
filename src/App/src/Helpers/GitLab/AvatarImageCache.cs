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

      public Image GetAvatar(User user)
      {
         if (user == null || _dataCache.AvatarCache == null)
         {
            return null;
         }

         Key key = new Key(user.Id);
         if (_cached.TryGetValue(key, out Image image))
         {
            return image;
         }

         image = convertByteToImage(_dataCache.AvatarCache.GetAvatar(user));
         if (image == null)
         {
            return Properties.Resources.loading_transp_alpha;
         }

         _cached[key] = image;
         return image;
      }

      private Image convertByteToImage(byte[] bytes)
      {
         if (bytes == null)
         {
            return null;
         }

         using (System.IO.MemoryStream ms = new System.IO.MemoryStream(bytes))
         {
            return Image.FromStream(ms);
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
         public Key(int userId)
         {
            _userId = userId;
         }

         private int _userId;

         public override bool Equals(object obj)
         {
            return obj is Key key && Equals(key);
         }

         public bool Equals(Key other)
         {
            return _userId == other._userId;
         }

         public override int GetHashCode()
         {
            int hashCode = -1622434987;
            hashCode = hashCode * -1521134295 + _userId.GetHashCode();
            return hashCode;
         }
      }
      private readonly Dictionary<Key, Image> _cached = new Dictionary<Key, Image>();
   }
}

