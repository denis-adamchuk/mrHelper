using System.Threading.Tasks;
using GitLabSharp;
using GitLabSharp.Accessors;

namespace mrHelper.CustomActions
{
   public class MergeRequestEndPointPOSTCommand : BaseCommand
   {
      public MergeRequestEndPointPOSTCommand(
         ICommandCallback callback,
         string name,
         string endpoint,
         string enabledIf,
         string visibleIf,
         bool stopTimer,
         bool reload,
         string hint)
         : base(callback, name, enabledIf, visibleIf, stopTimer, reload, hint)
      {
         _endpoint = endpoint;
      }

      async public override Task Run()
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
   }
}

