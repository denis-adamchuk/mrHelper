using System.Threading.Tasks;
using GitLabSharp;
using GitLabSharp.Accessors;
using mrHelper.Client.Session;

namespace mrHelper.Client.Common
{
   public class GitLabClientManager
   {
      public GitLabClientManager(GitLabClientContext clientContext)
      {
         GitLabAccessor = new GitLabAccessor(clientContext.HostProperties);
         SessionManager = new SessionManager(clientContext, GitLabAccessor.ModificationNotifier);
      }

      public ISessionManager SessionManager { get; }
      public IGitLabAccessor GitLabAccessor { get; }
   }
}

