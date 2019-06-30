using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrHelper
{
   class SendCommentCommand : ICommand
   {
      public SendCommentCommand(ICommandCallback callback, string name, string comment)
      {
         _callback = callback;
         _name = name;
         _comment = comment;
      }
      
      public string GetName()
      {
         return _name;
      }

      public void Run(object sender, System.EventArgs e)
      {
         gitlabClient client = new gitlabClient(_callback.GetCurrentHostName(), _callback.GetCurrentAccessToken());
         DiscussionParameters parameters;
         parameters.Body = _comment;
         parameters.Position = null;
         client.CreateNewMergeRequestDiscussion(
            _callback.GetCurrentProjectName(), _callback.GetCurrentMergeRequestId(), parameters); 
      }

      ICommandCallback _callback;
      string _name;
      string _comment;
   }
}
