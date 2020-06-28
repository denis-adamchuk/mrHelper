using mrHelper.Client.Common;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.Repository
{
   internal class RepositoryManager
   {
      internal RepositoryManager(GitLabClientContext clientContext, string hostname)
      {
         _hostname = hostname;
         _settings = clientContext.HostProperties;
      }

      public IRepositoryAccessor GetRepositoryAccessor()
      {
         return new RepositoryAccessor(_settings, _hostname);
      }

      private readonly string _hostname;
      private readonly IHostProperties _settings;
   }
}

