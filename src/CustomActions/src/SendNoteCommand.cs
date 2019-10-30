using System;
using System.Threading.Tasks;
using GitLabSharp;
using GitLabSharp.Accessors;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;

namespace mrHelper.CustomActions
{
   public class SendNoteCommand : ICommand
   {
      public SendNoteCommand(ICommandCallback callback, string name, string body, string dependency)
      {
         _callback = callback;
         _name = name;
         _body = body;
         _dependency = dependency;
      }

      public string GetName()
      {
         return _name;
      }

      public string GetBody()
      {
         return _body;
      }

      public string GetDependency()
      {
         return _dependency;
      }

      async public Task Run()
      {
         GitLabClient client = new GitLabClient(_callback.GetCurrentHostName(), _callback.GetCurrentAccessToken());
         await client.RunAsync(async (gitlab) =>
            await gitlab.Projects.Get(_callback.GetCurrentProjectName()).MergeRequests.
               Get(_callback.GetCurrentMergeRequestIId()).
                  Notes.CreateNewTaskAsync(new CreateNewNoteParameters
                  {
                     Body = _body
                  }));
         client.Dispose();
      }

      private readonly ICommandCallback _callback;
      private readonly string _name;
      private readonly string _body;
      private readonly string _dependency;
   }
}
