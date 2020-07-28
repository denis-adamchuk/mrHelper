using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient.Accessors;

namespace mrHelper.GitLabClient
{
   public class GitLabInstance
   {
      public GitLabInstance(string hostname, IHostProperties hostProperties)
      {
         HostProperties = hostProperties;
         HostName = hostname;
      }

      internal ModificationNotifier ModificationNotifier { get; } = new ModificationNotifier();
      internal IHostProperties HostProperties { get; }
      internal string HostName { get; }
   }
}

