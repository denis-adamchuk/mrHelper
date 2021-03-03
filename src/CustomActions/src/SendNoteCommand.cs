using System.Threading.Tasks;
using GitLabSharp;
using GitLabSharp.Accessors;

namespace mrHelper.CustomActions
{
   public class SendNoteCommand : BaseCommand
   {
      public SendNoteCommand(
         ICommandCallback callback,
         string name,
         string body,
         string enabledIf,
         string visibleIf,
         bool stopTimer,
         bool reload,
         string hint,
         bool initiallyVisible)
         : base(callback, name, enabledIf, visibleIf, stopTimer, reload, hint, initiallyVisible)
      {
         Body = body;
      }

      async public override Task Run()
      {
         string hostname = _callback.GetCurrentHostName();
         string accessToken = _callback.GetCurrentAccessToken();
         string projectName = _callback.GetCurrentProjectName();
         int iid = _callback.GetCurrentMergeRequestIId();

         GitLabTaskRunner client = new GitLabTaskRunner(hostname, accessToken);
         await client.RunAsync(async (gitlab) =>
            await gitlab.Projects.Get(projectName).MergeRequests.
               Get(iid).Notes.CreateNewTaskAsync(new CreateNewNoteParameters(Body)));
      }

      public string Body { get; }
   }
}

