using mrHelper.Client.MergeRequests;
using mrHelper.Client.Repository;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.Projects
{
   internal class SingleProjectAccessor : ISingleProjectAccessor
   {
      public SingleProjectAccessor(ProjectKey projectKey, IHostProperties settings)
      {
         _projectKey = projectKey;
         _settings = settings;
      }

      public IRepositoryAccessor RepositoryAccessor =>
         new RepositoryAccessor(_settings, _projectKey);

      public IMergeRequestAccessor MergeRequestAccessor =>
         new MergeRequestAccessor(_settings, _projectKey);

      private readonly ProjectKey _projectKey;
      private readonly IHostProperties _settings;
   }
}

