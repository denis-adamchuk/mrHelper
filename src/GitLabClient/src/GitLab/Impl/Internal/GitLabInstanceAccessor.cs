using mrHelper.Client.Projects;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.Common
{
   internal class GitLabInstanceAccessor : IGitLabInstanceAccessor
   {
      public GitLabInstanceAccessor(string hostname, IHostProperties hostProperties)
      {
         _hostname = hostname;
         _hostProperties = hostProperties;
      }

      public IProjectAccessor ProjectAccessor =>
         new ProjectAccessor(_hostProperties, _hostname);

      private readonly string _hostname;
      private readonly IHostProperties _hostProperties;
   }
}
