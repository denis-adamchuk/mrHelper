using GitLabSharp.Entities;
using mrHelper.GitLabClient.Loaders.Cache;

namespace mrHelper.GitLabClient.Managers
{
   // Unlike ProjectCache and UserCache, AvatarCache does not embed AvatarLoader.
   // AvatarLoader objects are embedded into other Loaders to load avatars for
   // specific users only (unlike ProjectCache and UserCache which force loading full lists).
   internal class AvatarCache : IAvatarCache
   {
      public AvatarCache(IInternalCache cache)
      {
         _cache = cache;
         _diskCache = new AvatarDiskCache();
      }

      public byte[] GetAvatar(User user) =>
         _cache.GetAvatar(user.Id) ?? readAvatarFromDisk(user);

      private byte[] readAvatarFromDisk(User user) =>
         _diskCache.LoadUserAvatar(user);

      private IInternalCache _cache;
      private readonly AvatarDiskCache _diskCache;
   }
}

