using System.Threading.Tasks;
using GitLabSharp;
using GitLabSharp.Accessors;

namespace mrHelper.CustomActions
{
   internal class SendNoteCommand : ISubCommand
   {
      internal SendNoteCommand(string body)
      {
         _body = body;
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
            NoteAccessor accessor = gitlab.Projects.Get(projectName).MergeRequests.Get(iid).Notes;
            accessor.TraceRequests = true;
            return await accessor.CreateNewTaskAsync(new CreateNewNoteParameters(_body));
         });
      }

      private readonly string _body;
   }
}

