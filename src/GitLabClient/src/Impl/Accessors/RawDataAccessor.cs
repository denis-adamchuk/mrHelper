using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient.Interfaces;

namespace mrHelper.GitLabClient
{
   public class RawDataAccessor
   {
      public RawDataAccessor(GitLabInstance gitLabInstance,
         INetworkOperationStatusListener networkOperationStatusListener)
      {
         _hostname = gitLabInstance.HostName;
         _hostProperties = gitLabInstance.HostProperties;
         _networkOperationStatusListener = networkOperationStatusListener;
      }

      public ProjectAccessor GetProjectAccessor(IModificationListener modificationListener)
      {
         return new ProjectAccessor(_hostProperties, _hostname, modificationListener, _networkOperationStatusListener);
      }

      public UserAccessor UserAccessor =>
         new UserAccessor(_hostname, _hostProperties, _networkOperationStatusListener);

      private readonly string _hostname;
      private readonly IHostProperties _hostProperties;
      private readonly INetworkOperationStatusListener _networkOperationStatusListener;
   }
}

