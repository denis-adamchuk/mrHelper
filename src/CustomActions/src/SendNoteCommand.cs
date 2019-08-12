using System.Threading.Tasks;
using GitLabSharp;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;

namespace mrHelper.CustomActions
{
   public class SendNoteCommand : ICommand
   {
      public SendNoteCommand(ICommandCallback callback, string name, string body)
      {
         _callback = callback;
         _name = name;
         _body = body;
      }

      public string GetName()
      {
         return _name;
      }

      async public Task Run()
      {
         GitLabClient client = new GitLabClient(_callback.GetCurrentHostName(), _callback.GetCurrentAccessToken());
         client.RunAsync(async (gitlab) =>
            await gitlab.Projects.Get(_callback.GetCurrentProjectName()).MergeRequests.
               Get(_callback.GetCurrentMergeRequestIId()).
                  Notes.CreateNewTaskAsync(new CreateNewNoteParameters
                  {
                     Body = _body
                  }));
      }

      private readonly ICommandCallback _callback;
      private readonly string _name;
      private readonly string _body;
   }
}
