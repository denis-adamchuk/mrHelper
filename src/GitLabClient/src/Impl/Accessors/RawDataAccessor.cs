using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient.Interfaces;

namespace mrHelper.GitLabClient
{
   public class RawDataAccessor
   {
      public RawDataAccessor(GitLabInstance gitLabInstance,
         IConnectionLossListener connectionLossListener)
      {
         _hostname = gitLabInstance.HostName;
         _hostProperties = gitLabInstance.HostProperties;
         _connectionLossListener = connectionLossListener;
      }

      public ProjectAccessor GetProjectAccessor(IModificationListener modificationListener)
      {
         return new ProjectAccessor(_hostProperties, _hostname, modificationListener, _connectionLossListener);
      }

      public UserAccessor UserAccessor =>
         new UserAccessor(_hostname, _hostProperties, _connectionLossListener);

      private readonly string _hostname;
      private readonly IHostProperties _hostProperties;
      private readonly IConnectionLossListener _connectionLossListener;
   }
}

