using GitLabSharp;

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

      public void Run()
      {
         GitLab gl = new GitLab(_callback.GetCurrentHostName(), _callback.GetCurrentAccessToken());
         gl.Projects.Get(_callback.GetCurrentProjectName()).MergeRequests.Get(_callback.GetCurrentMergeRequestId()).
            Notes.CreateNew(new CreateNewNoteParameters
            {
               Body = _body
            });
      }

      private readonly ICommandCallback _callback;
      private readonly string _name;
      private readonly string _body;
   }
}
