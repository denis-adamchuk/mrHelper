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
         string host = _callback.GetCurrentHostName();
         string accessToken = _callback.GetCurrentAccessToken();
         string project = _callback.GetCurrentProjectName();
         int mrId = _callback.GetCurrentMergeRequestId();
         if (host == null || accessToken == null || project == null || mrId == 0)
         {
            return;
         }

         gitlabClient client = new gitlabClient(host, accessToken);
         DiscussionParameters parameters;
         parameters.Body = _comment;
         parameters.Position = null;
         client.CreateNewMergeRequestDiscussion(project, mrId, parameters); 
      }

      ICommandCallback _callback;
      string _name;
      string _comment;
   }
}
