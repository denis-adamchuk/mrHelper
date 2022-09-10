using GitLabSharp.Entities;
using mrHelper.GitLabClient.Loaders.Cache;

namespace mrHelper.GitLabClient.Managers
{
   internal class AvatarCache : IAvatarCache
   {
      public AvatarCache(IInternalCache cache)
      {
         _cache = cache;
      }

      public byte[] GetAvatar(User user)
      {
         return _cache.GetAvatar(user.Id);
      }

      private IInternalCache _cache;
   }
}

