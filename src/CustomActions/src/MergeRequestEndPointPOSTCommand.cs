using System.Threading.Tasks;
using GitLabSharp;
using GitLabSharp.Accessors;

namespace mrHelper.CustomActions
{
   internal class MergeRequestEndPointPOSTCommand : ISubCommand
   {
      internal MergeRequestEndPointPOSTCommand(ICommandCallback callback, string endpoint)
      {
         _callback = callback;
         _endpoint = endpoint;
      }

      async public Task Run()
      {
         string hostname = _callback.GetCurrentHostName();
         string accessToken = _callback.GetCurrentAccessToken();
         string projectName = _callback.GetCurrentProjectName();
         int iid = _callback.GetCurrentMergeRequestIId();

         GitLabTaskRunner client = new GitLabTaskRunner(hostname, accessToken);
         await client.RunAsync(async (gitlab) =>
         {
            SingleMergeRequestAccessor accessor = gitlab.Projects.Get(projectName).MergeRequests.Get(iid);
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

      private readonly ICommandCallback _callback;
   }
}

