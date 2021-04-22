using System.Threading.Tasks;
using GitLabSharp;
using GitLabSharp.Accessors;

namespace mrHelper.CustomActions
{
   internal class MergeRequestEndPointPOSTCommand : ISubCommand
   {
      internal MergeRequestEndPointPOSTCommand(string endpoint)
      {
         _endpoint = endpoint;
      }

      async public Task Run(ICommandCallback callback)
      {
         string hostname = callback.GetCurrentHostName();
         string accessToken = callback.GetCurrentAccessToken();
         string projectName = callback.GetCurrentProjectName();
         int iid = callback.GetCurrentMergeRequestIId();

         GitLabTaskRunner client = new GitLabTaskRunner(hostname, accessToken);
         await client.RunAsync(async (gitlab) =>
         {
            SingleMergeRequestAccessor accessor = gitlab.Projects.Get(projectName).MergeRequests.Get(iid);
            accessor.TraceRequests = true;
            if (_endpoint == "approve")
            {
               return await accessor.ApproveTaskAsync();
            }
            else if (_endpoint == "unapprove")
            {
               return await accessor.UnapproveTaskAsync();
            }
            return null;
         });
      }

      private readonly string _endpoint;
   }
}

