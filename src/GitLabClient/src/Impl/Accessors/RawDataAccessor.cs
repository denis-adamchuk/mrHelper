using mrHelper.Common.Interfaces;

namespace mrHelper.GitLabClient
{
   public class RawDataAccessor
   {
      public RawDataAccessor(GitLabInstance gitLabInstance, IModificationListener modificationListener)
      {
         _hostname = gitLabInstance.HostName;
         _hostProperties = gitLabInstance.HostProperties;
         _modificationListener = modificationListener;
      }

      public ProjectAccessor ProjectAccessor =>
         new ProjectAccessor(_hostProperties, _hostname, _modificationListener);

      public UserAccessor UserAccessor =>
         new UserAccessor(_hostname, _hostProperties);

      private readonly string _hostname;
      private readonly IHostProperties _hostProperties;
      private readonly IModificationListener _modificationListener;
   }
}

