using System.Linq;
using System.Threading.Tasks;
using GitLabSharp;
using GitLabSharp.Accessors;

namespace mrHelper.CustomActions
{
   internal class AddLabelToMergeRequest : ISubCommand
   {
      internal AddLabelToMergeRequest(ICommandCallback callback, string label)
      {
         _callback = callback;
         _label = label;
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
            if (_label == Common.Constants.Constants.HighPriorityLabel)
            {
               SingleMergeRequestAccessor accessor = gitlab.Projects.Get(projectName).MergeRequests.Get(iid);
               GitLabSharp.Entities.MergeRequest mergeRequest = await accessor.LoadTaskAsync(); 
               bool wasHighPriority = mergeRequest.Labels?
                  .Contains(Common.Constants.Constants.HighPriorityLabel) ?? false;
               if (!wasHighPriority)
               {
                  string[] labels = mergeRequest.Labels
                     .Concat(new string[] { Common.Constants.Constants.HighPriorityLabel }).ToArray();
                  UpdateMergeRequestParameters updateMergeRequestParameters = new UpdateMergeRequestParameters(
                     null, null, null, null, null, null, null, labels);
                  await accessor.UpdateMergeRequestTaskAsync(updateMergeRequestParameters);
               }
            }
            return null;
         });
      }

      private readonly string _label;

      private readonly ICommandCallback _callback;
   }
}

