using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient.Accessors;

namespace mrHelper.GitLabClient
{
   public class RawDataAccessor
   {
      public RawDataAccessor(GitLabInstance gitLabInstance)
      {
         _hostname = gitLabInstance.HostName;
         _hostProperties = gitLabInstance.HostProperties;
         _modificationNotifier = gitLabInstance.ModificationNotifier;
      }

      public ProjectAccessor ProjectAccessor =>
         new ProjectAccessor(_hostProperties, _hostname, _modificationNotifier);

      public UserAccessor UserAccessor =>
         new UserAccessor(_hostname, _hostProperties);

      private readonly string _hostname;
      private readonly IHostProperties _hostProperties;
      private readonly ModificationNotifier _modificationNotifier;
   }
}

