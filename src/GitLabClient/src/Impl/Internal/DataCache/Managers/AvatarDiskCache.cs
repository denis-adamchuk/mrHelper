using System;
using GitLabSharp.Entities;
using mrHelper.Common.Tools;

namespace mrHelper.GitLabClient.Managers
{
   internal class AvatarDiskCache : DiskCache
   {
      internal AvatarDiskCache()
         : base(PathFinder.AvatarStorage)
      {
      }

      internal bool IsUserAvatarCached(User user)
      {
         string avatarUrl = getAvatarUrl(user);
         if (avatarUrl == null)
         {
            return false;
         }

         string key = getAvatarCacheKey(avatarUrl);
         return base.IsUserAvatarCached(key);
      }

      internal byte[] LoadUserAvatar(User user)
      {
         string avatarUrl = getAvatarUrl(user);
         if (avatarUrl == null)
         {
            return null;
         }

         string key = getAvatarCacheKey(avatarUrl);
         try
         {
            return base.LoadBytes(key);
         }
         catch (DiskCacheReadException ex)
         {
            Common.Exceptions.ExceptionHandlers.Handle("Cannot read avatar from disk", ex);
         }
         return null;
      }

      internal void SaveUserAvatar(User user, byte[] bytes)
      {
         string avatarUrl = getAvatarUrl(user);
         if (avatarUrl == null)
         {
            return;
         }

         string key = getAvatarCacheKey(avatarUrl);
         try
         {
            base.SaveBytes(key, bytes);
         }
         catch (DiskCacheWriteException ex)
         {
            Common.Exceptions.ExceptionHandlers.Handle("Cannot write avatar to disk", ex);
         }
      }

      private string getAvatarUrl(User user)
      {
         bool isValidUrl = !String.IsNullOrWhiteSpace(user.Avatar_Url);
         return isValidUrl ? user.Avatar_Url : null;
      }

      private string getAvatarCacheKey(string avatarUrl) =>
         CryptoHelper.GetHashString(avatarUrl);
   }
}

