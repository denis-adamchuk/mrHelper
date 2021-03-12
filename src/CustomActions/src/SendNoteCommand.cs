using System.Threading.Tasks;
using GitLabSharp;
using GitLabSharp.Accessors;

namespace mrHelper.CustomActions
{
   internal class SendNoteCommand : ISubCommand
   {
      internal SendNoteCommand(ICommandCallback callback, string body)
      {
         _callback = callback;
         _body = body;
      }

      async public Task Run()
      {
         string hostname = _callback.GetCurrentHostName();
         string accessToken = _callback.GetCurrentAccessToken();
         string projectName = _callback.GetCurrentProjectName();
         int iid = _callback.GetCurrentMergeRequestIId();

         GitLabTaskRunner client = new GitLabTaskRunner(hostname, accessToken);
         await client.RunAsync(async (gitlab) =>
            await gitlab.Projects.Get(projectName).MergeRequests.
               Get(iid).Notes.CreateNewTaskAsync(new CreateNewNoteParameters(_body)));
      }

      private readonly string _body;

      private readonly ICommandCallback _callback;
   }
}

