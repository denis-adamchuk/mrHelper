using mrHelper.Common.Interfaces;

namespace mrHelper.GitLabClient
{
   public class RawDataAccessor
   {
      public RawDataAccessor(GitLabInstance gitLabInstance)
      {
         _hostname = gitLabInstance.HostName;
         _hostProperties = gitLabInstance.HostProperties;
         _modificationListener = gitLabInstance.ModificationListener;
         _networkOperationStatusListener = gitLabInstance.NetworkOperationStatusListener;
      }

      public ProjectAccessor GetProjectAccessor()
      {
         return new ProjectAccessor(_hostProperties, _hostname, _modificationListener, _networkOperationStatusListener);
      }

      public UserAccessor UserAccessor =>
         new UserAccessor(_hostname, _hostProperties, _networkOperationStatusListener);

      private readonly string _hostname;
      private readonly IHostProperties _hostProperties;
      private readonly IModificationListener _modificationListener;
      private readonly INetworkOperationStatusListener _networkOperationStatusListener;
   }
}

