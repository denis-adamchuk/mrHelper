using mrHelper.Client.Projects;

namespace mrHelper.Client.Common
{
   public interface IGitLabInstanceAccessor
   {
      IProjectAccessor ProjectAccessor { get; }
   }
}

