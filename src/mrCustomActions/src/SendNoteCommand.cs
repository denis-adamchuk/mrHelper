using GitLabSharp;
using System.Threading.Tasks;

namespace mrCustomActions
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
         GitLab gl = new GitLab(_callback.GetCurrentHostName(), _callback.GetCurrentAccessToken());
         await gl.Projects.Get(_callback.GetCurrentProjectName()).MergeRequests.Get(_callback.GetCurrentMergeRequestId()).
            Notes.CreateNewTaskAsync(new CreateNewNoteParameters
            {
               Body = _body
            });
      }

      private readonly ICommandCallback _callback;
      private readonly string _name;
      private readonly string _body;
   }
}
