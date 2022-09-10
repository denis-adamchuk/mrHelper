using GitLabSharp.Entities;

namespace mrHelper.GitLabClient
{
   public interface IAvatarCache
   {
      byte[] GetAvatar(User user);
   }
}

