using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient.Accessors;

namespace mrHelper.GitLabClient
{
   public class SingleProjectAccessor
   {
      internal SingleProjectAccessor(ProjectKey projectKey, IHostProperties settings,
         IModificationListener modificationListener)
      {
         _projectKey = projectKey;
         _settings = settings;
         _modificationListener = modificationListener;
      }

      public RepositoryAccessor GetRepositoryAccessor() =>
         new RepositoryAccessor(_settings, _projectKey);

      public MergeRequestAccessor MergeRequestAccessor =>
         new MergeRequestAccessor(_settings, _projectKey, _modificationListener);

      private readonly ProjectKey _projectKey;
      private readonly IHostProperties _settings;
      private readonly IModificationListener _modificationListener;
   }
}

