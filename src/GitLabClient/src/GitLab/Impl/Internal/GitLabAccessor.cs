using mrHelper.Common.Interfaces;

namespace mrHelper.Client.Common
{
   internal class GitLabAccessor : IGitLabAccessor
   {
      public GitLabAccessor(IHostProperties hostProperties)
      {
         _hostProperties = hostProperties;
         _modificationNotifier = new ModificationNotifier();
      }

      public IGitLabInstanceAccessor GetInstanceAccessor(string hostname)
      {
         return new GitLabInstanceAccessor(hostname, _hostProperties, _modificationNotifier);
      }

      public IModificationNotifier ModificationNotifier => _modificationNotifier;

      private readonly IHostProperties _hostProperties;
      private readonly ModificationNotifier _modificationNotifier;
   }
}

