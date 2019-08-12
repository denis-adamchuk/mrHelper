using System;
using GitLabSharp;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;

namespace mrHelper.Client.Discussions
{
   /// <summary>
   /// Implements Discussions-related interaction with GitLab
   /// </summary>
   internal class DiscussionOperator
   {
      internal DiscussionOperator(UserDefinedSettings settings) 
      {
         Settings = settings;
      }

      async internal Task<List<Discussion>> GetDiscussionsAsync(MergeRequestDescriptor mrd)
      {
         GitLabClient client = new GitLabClient(mrd.HostName, Tools.GetAccessToken(mrd.HostName, Settings));
         try
         {
            return await client.RunAsync(async (gitlab) =>
               return await gitlab.Projects.Get(mrd.ProjectName).MergeRequests.Get(mrd.IId).
                  Discussions.LoadAllTaskAsync());
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot load discussions from GitLab");
            throw new OperatorException(ex);
         }
      }

      async internal Task<Discussion> GetDiscussionAsync(MergeRequestDescriptor mrd, string discussionId)
      {
         GitLabClient client = new GitLabClient(mrd.HostName, Tools.GetAccessToken(mrd.HostName, Settings));
         try
         {
            return await client.RunAsync(async (gitlab) =>
               return await gitlab.Projects.Get(mrd.ProjectName).MergeRequests.Get(mrd.IId).
                  Discussions.Get(discussionId).LoadTaskAsync());
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot load discussion from GitLab");
            throw new OperatorException(ex);
         }
      }

      async internal Task ReplyAsync(MergeRequestDescriptor mrd, int discussionId, string body)
      {
         GitLabClient client = new GitLabClient(mrd.HostName, Tools.GetAccessToken(mrd.HostName, Settings));
         try
         {
            await client.RunAsync(async (gitlab) =>
               return await gitlab.Projects.Get(mrd.ProjectName).MergeRequests.Get(mrd.IId).
                  Discussions.Get(_discussionId).CreateNewNoteTaskAsync(
                     new CreateNewNoteParameters
                     {
                        Body = body
                     });
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot create a reply to discussion");
            throw new OperatorException(ex);
         }
      }

      async internal Task ModifyNoteBodyAsync(MergeRequestDescriptor mrd, string discussionId, int noteId, string body)
      {
         GitLabClient client = new GitLabClient(mrd.HostName, Tools.GetAccessToken(mrd.HostName, Settings));
         try
         {
            await client.RunAsync(async (gitlab) =>
               return await gitlab.Projects.Get(mrd.ProjectName).MergeRequests.Get(mrd.IId).
                  Discussions.Get(discussionId).ModifyNoteTaskAsync(noteId,
                     new ModifyDiscussionNoteParameters
                     {
                        Type = ModifyDiscussionNoteParameters.ModificationType.Body,
                        Body = textBox.Text
                     }));
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot update discussion text");
            throw new OperatorException(ex);
         }
      }

      async internal Task DeleteNoteAsync(MergeRequestDescriptor mrd, string discussionId, int noteId)
      {
         try
         {
            await client.RunAsync(async (gitlab) =>
               return await gitlab.Projects.Get(mrd.ProjectName).MergeRequests.Get(mrd.IId).
                  Notes.Get(noteId).DeleteTaskAsync());
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot delete a note");
            throw new OperatorException(ex);
         }
      }

      async internal Task ResolveNoteAsync(MergeRequestDescriptor mrd, string discussionId, int noteId)
      {
         GitLabClient client = new GitLabClient(mrd.HostName, Tools.GetAccessToken(mrd.HostName, Settings));
         try
         {
            await client.RunAsync(async (gitlab) =>
               return await gitlab.Projects.Get(mrd.ProjectName).MergeRequests.Get(mrd.IId).
                  Discussions.Get(discussionId).ModifyNoteTaskAsync(noteId,
                     new ModifyDiscussionNoteParameters
                     {
                        Type = ModifyDiscussionNoteParameters.ModificationType.Resolved,
                        Resolved = !wasResolved
                     }));
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot toggle 'Resolved' state of a note");
            throw new OperatorException(ex);
         }
      }

      async internal Task ResolveDiscussionAsync(MergeRequestDescriptor mrd, string discussionId)
      {
         GitLabClient client = new GitLabClient(mrd.HostName, Tools.GetAccessToken(mrd.HostName, Settings));
         try
         {
            await client.RunAsync(async (gitlab) =>
               return await gitlab.Projects.Get(mrd.ProjectName).MergeRequests.Get(mrd.IId).
                  Discussions.Get(discussionId).ResolveTaskAsync(
                     new ResolveThreadParameters
                     {
                        Resolve = !wasResolved
                     }));
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot toggle 'Resolved' state of a discussion");
            throw new OperatorException(ex);
         }
      }

      async internal Task CreateDiscussionAsync(MergeRequestDescriptor mrd, NewDiscussionParameters parameters)
      {
         GitLabClient client = new GitLabClient(mrd.HostName, Tools.GetAccessToken(mrd.HostName, Settings));
         try
         {
            await client.RunAsync(async (gitlab) =>
               return await gitlab.Projects.Get(mrd.ProjectName).MergeRequests.Get(mrd.IId).
                  Discussions.CreateNew(parameters));
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot create a discussion");
            throw new OperatorException(ex);
         }
      }

      private Settings Settings { get; }
   }
}

