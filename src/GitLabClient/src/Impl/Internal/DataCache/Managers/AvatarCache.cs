using GitLabSharp.Entities;
using mrHelper.Common.Tools;
using mrHelper.GitLabClient.Loaders.Cache;

namespace mrHelper.GitLabClient.Managers
{
   internal class AvatarCache : IAvatarCache
   {
      public AvatarCache(IInternalCache cache)
      {
         _cache = cache;
         _diskCache = new DiskCache(PathFinder.AvatarStorage);
      }

      public byte[] GetAvatar(User user)
      {
         return _cache.GetAvatar(user.Id) ?? readAvatarFromDisk(user.Avatar_Url);
      }

      private string getAvatarCacheKey(string avatarUrl)
      {
         return CryptoHelper.GetHashString(avatarUrl);
      }

      private byte[] readAvatarFromDisk(string avatarUrl)
      {
         string key = getAvatarCacheKey(avatarUrl);
         try
         {
            return _diskCache.LoadBytes(key);
         }
         catch (DiskCacheReadException ex)
         {
            Common.Exceptions.ExceptionHandlers.Handle("Cannot read avatar from disk", ex);
         }
         return null;
      }

      private IInternalCache _cache;
      private readonly DiskCache _diskCache;
   }
}

