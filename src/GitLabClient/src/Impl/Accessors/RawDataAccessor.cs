using mrHelper.Common.Interfaces;

namespace mrHelper.GitLabClient
{
   public class RawDataAccessor
   {
      public RawDataAccessor(GitLabInstance gitLabInstance)
      {
         _hostname = gitLabInstance.HostName;
         _hostProperties = gitLabInstance.HostProperties;
      }

      public ProjectAccessor GetProjectAccessor(IModificationListener modificationListener)
      {
         return new ProjectAccessor(_hostProperties, _hostname, modificationListener);
      }

      public UserAccessor UserAccessor =>
         new UserAccessor(_hostname, _hostProperties);

      private readonly string _hostname;
      private readonly IHostProperties _hostProperties;
   }
}

