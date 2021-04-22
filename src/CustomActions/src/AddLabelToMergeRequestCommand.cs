using System.Linq;
using System.Threading.Tasks;
using GitLabSharp;
using GitLabSharp.Accessors;

namespace mrHelper.CustomActions
{
   internal class AddLabelToMergeRequestCommand : ISubCommand
   {
      internal AddLabelToMergeRequestCommand(string label)
      {
         _label = label;
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
            if (_label == Common.Constants.Constants.HighPriorityLabel)
            {
               SingleMergeRequestAccessor accessor = gitlab.Projects.Get(projectName).MergeRequests.Get(iid);
               accessor.TraceRequests = true;
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
   }
}

