using System.Threading.Tasks;
using mrHelper.Client.Projects;

namespace mrHelper.Client.Common
{
   public enum ConnectionCheckStatus
   {
      OK,
      BadHostname,
      BadAccessToken
   }

   public interface IGitLabInstanceAccessor
   {
      IProjectAccessor ProjectAccessor { get; }

      IUserAccessor UserAccessor { get; }

      Task<ConnectionCheckStatus> VerifyConnection(string token);
   }
}

