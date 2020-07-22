using mrHelper.Common.Interfaces;

namespace mrHelper.Client.Common
{
   internal class GitLabAccessor : IGitLabAccessor
   {
      public GitLabAccessor(IHostProperties hostProperties)
      {
         _hostProperties = hostProperties;
      }

      public IGitLabInstanceAccessor GetInstanceAccessor(string hostname)
      {
         return new GitLabInstanceAccessor(hostname, _hostProperties);
      }

      private readonly IHostProperties _hostProperties;
   }
}

