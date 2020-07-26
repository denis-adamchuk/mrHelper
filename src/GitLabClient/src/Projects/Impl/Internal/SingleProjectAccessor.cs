using mrHelper.Client.Common;
using mrHelper.Client.MergeRequests;
using mrHelper.Client.Repository;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.Projects
{
   internal class SingleProjectAccessor : ISingleProjectAccessor
   {
      public SingleProjectAccessor(ProjectKey projectKey, IHostProperties settings,
         ModificationNotifier modificationNotifier)
      {
         _projectKey = projectKey;
         _settings = settings;
         _modificationNotifier = modificationNotifier;
      }

      public IRepositoryAccessor RepositoryAccessor =>
         new RepositoryAccessor(_settings, _projectKey);

      public IMergeRequestAccessor MergeRequestAccessor =>
         new MergeRequestAccessor(_settings, _projectKey, _modificationNotifier);

      private readonly ProjectKey _projectKey;
      private readonly IHostProperties _settings;
      private readonly ModificationNotifier _modificationNotifier;
   }
}

