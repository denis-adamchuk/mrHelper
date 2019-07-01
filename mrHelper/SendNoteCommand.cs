using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrHelper
{
   class SendNoteCommand : ICommand
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

      public void Run(object sender, System.EventArgs e)
      {
         gitlabClient client = new gitlabClient(_callback.GetCurrentHostName(), _callback.GetCurrentAccessToken());
         client.CreateNewMergeRequestNote(
            _callback.GetCurrentProjectName(), _callback.GetCurrentMergeRequestId(), _body); 
      }

      ICommandCallback _callback;
      string _name;
      string _body;
   }
}
