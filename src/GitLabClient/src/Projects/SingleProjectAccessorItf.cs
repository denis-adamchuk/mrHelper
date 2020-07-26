using mrHelper.Client.MergeRequests;
using mrHelper.Client.Repository;

namespace mrHelper.Client.Projects
{
   public interface ISingleProjectAccessor
   {
      IRepositoryAccessor RepositoryAccessor { get; }
      IMergeRequestAccessor MergeRequestAccessor { get; }
   }
}

