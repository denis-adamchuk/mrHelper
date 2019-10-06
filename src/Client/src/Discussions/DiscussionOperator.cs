using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp;
using GitLabSharp.Accessors;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;
using System.Diagnostics;

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

      async internal Task<List<Discussion>> GetDiscussionsAsync(MergeRequestKey mrk)
      {
         GitLabClient client = new GitLabClient(mrk.ProjectKey.HostName, Settings.GetAccessToken(mrk.ProjectKey.HostName));
         try
         {
            return (List<Discussion>)(await client.RunAsync(async (gitlab) =>
               await gitlab.Projects.Get(mrk.ProjectKey.ProjectName).MergeRequests.Get(mrk.IId).
                  Discussions.LoadAllTaskAsync()));
         }
         catch (Exception ex)
         {
            Debug.Assert(!(ex is GitLabClientCancelled));
            if (ex is GitLabSharpException || ex is GitLabRequestException)
            {
               ExceptionHandlers.Handle(ex, "Cannot load discussions from GitLab");
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      async internal Task<Discussion> GetDiscussionAsync(MergeRequestKey mrk, string discussionId)
      {
         GitLabClient client = new GitLabClient(mrk.ProjectKey.HostName, Settings.GetAccessToken(mrk.ProjectKey.HostName));
         try
         {
            return (Discussion)(await client.RunAsync(async (gitlab) =>
               await gitlab.Projects.Get(mrk.ProjectKey.ProjectName).MergeRequests.Get(mrk.IId).
                  Discussions.Get(discussionId).LoadTaskAsync()));
         }
         catch (Exception ex)
         {
            Debug.Assert(!(ex is GitLabClientCancelled));
            if (ex is GitLabSharpException || ex is GitLabRequestException)
            {
               ExceptionHandlers.Handle(ex, "Cannot load discussion from GitLab");
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      async internal Task ReplyAsync(MergeRequestKey mrk, string discussionId, string body)
      {
         GitLabClient client = new GitLabClient(mrk.ProjectKey.HostName, Settings.GetAccessToken(mrk.ProjectKey.HostName));
         try
         {
            await client.RunAsync(async (gitlab) =>
               await gitlab.Projects.Get(mrk.ProjectKey.ProjectName).MergeRequests.Get(mrk.IId).
                  Discussions.Get(discussionId).CreateNewNoteTaskAsync(
                     new CreateNewNoteParameters
                     {
                        Body = body
                     }));
         }
         catch (Exception ex)
         {
            Debug.Assert(!(ex is GitLabClientCancelled));
            if (ex is GitLabSharpException || ex is GitLabRequestException)
            {
               ExceptionHandlers.Handle(ex, "Cannot create a reply to discussion");
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      async internal Task ModifyNoteBodyAsync(MergeRequestKey mrk, string discussionId, int noteId, string body)
      {
         GitLabClient client = new GitLabClient(mrk.ProjectKey.HostName, Settings.GetAccessToken(mrk.ProjectKey.HostName));
         try
         {
            await client.RunAsync(async (gitlab) =>
               await gitlab.Projects.Get(mrk.ProjectKey.ProjectName).MergeRequests.Get(mrk.IId).
                  Discussions.Get(discussionId).ModifyNoteTaskAsync(noteId,
                     new ModifyDiscussionNoteParameters
                     {
                        Type = ModifyDiscussionNoteParameters.ModificationType.Body,
                        Body = body
                     }));
         }
         catch (Exception ex)
         {
            Debug.Assert(!(ex is GitLabClientCancelled));
            if (ex is GitLabSharpException || ex is GitLabRequestException)
            {
               ExceptionHandlers.Handle(ex, "Cannot update discussion text");
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      async internal Task DeleteNoteAsync(MergeRequestKey mrk, int noteId)
      {
         GitLabClient client = new GitLabClient(mrk.ProjectKey.HostName, Settings.GetAccessToken(mrk.ProjectKey.HostName));
         try
         {
            await client.RunAsync(async (gitlab) =>
               await gitlab.Projects.Get(mrk.ProjectKey.ProjectName).MergeRequests.Get(mrk.IId).
                  Notes.Get(noteId).DeleteTaskAsync());
         }
         catch (Exception ex)
         {
            Debug.Assert(!(ex is GitLabClientCancelled));
            if (ex is GitLabSharpException || ex is GitLabRequestException)
            {
               ExceptionHandlers.Handle(ex, "Cannot delete a note");
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      async internal Task ResolveNoteAsync(MergeRequestKey mrk, string discussionId, int noteId, bool resolved)
      {
         GitLabClient client = new GitLabClient(mrk.ProjectKey.HostName, Settings.GetAccessToken(mrk.ProjectKey.HostName));
         try
         {
            await client.RunAsync(async (gitlab) =>
               await gitlab.Projects.Get(mrk.ProjectKey.ProjectName).MergeRequests.Get(mrk.IId).
                  Discussions.Get(discussionId).ModifyNoteTaskAsync(noteId,
                     new ModifyDiscussionNoteParameters
                     {
                        Type = ModifyDiscussionNoteParameters.ModificationType.Resolved,
                        Resolved = resolved
                     }));
         }
         catch (Exception ex)
         {
            Debug.Assert(!(ex is GitLabClientCancelled));
            if (ex is GitLabSharpException || ex is GitLabRequestException)
            {
               ExceptionHandlers.Handle(ex, "Cannot toggle 'Resolved' state of a note");
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      async internal Task ResolveDiscussionAsync(MergeRequestKey mrk, string discussionId, bool resolved)
      {
         GitLabClient client = new GitLabClient(mrk.ProjectKey.HostName, Settings.GetAccessToken(mrk.ProjectKey.HostName));
         try
         {
            await client.RunAsync(async (gitlab) =>
               await gitlab.Projects.Get(mrk.ProjectKey.ProjectName).MergeRequests.Get(mrk.IId).
                  Discussions.Get(discussionId).ResolveTaskAsync(
                     new ResolveThreadParameters
                     {
                        Resolve = resolved
                     }));
         }
         catch (Exception ex)
         {
            Debug.Assert(!(ex is GitLabClientCancelled));
            if (ex is GitLabSharpException || ex is GitLabRequestException)
            {
               ExceptionHandlers.Handle(ex, "Cannot toggle 'Resolved' state of a discussion");
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      async internal Task CreateDiscussionAsync(MergeRequestKey mrk, NewDiscussionParameters parameters)
      {
         GitLabClient client = new GitLabClient(mrk.ProjectKey.HostName, Settings.GetAccessToken(mrk.ProjectKey.HostName));
         try
         {
            await client.RunAsync(async (gitlab) =>
               await gitlab.Projects.Get(mrk.ProjectKey.ProjectName).MergeRequests.Get(mrk.IId).
                  Discussions.CreateNewTaskAsync(parameters));
         }
         catch (Exception ex)
         {
            Debug.Assert(!(ex is GitLabClientCancelled));
            if (ex is GitLabSharpException || ex is GitLabRequestException)
            {
               ExceptionHandlers.Handle(ex, "Cannot create a discussion");
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      async internal Task CreateNoteAsync(MergeRequestKey mrk, CreateNewNoteParameters parameters)
      {
         GitLabClient client = new GitLabClient(mrk.ProjectKey.HostName, Settings.GetAccessToken(mrk.ProjectKey.HostName));
         try
         {
            await client.RunAsync(async (gitlab) =>
               await gitlab.Projects.Get(mrk.ProjectKey.ProjectName).MergeRequests.Get(mrk.IId).
                  Notes.CreateNewTaskAsync(parameters));
         }
         catch (Exception ex)
         {
            Debug.Assert(!(ex is GitLabClientCancelled));
            if (ex is GitLabSharpException || ex is GitLabRequestException)
            {
               ExceptionHandlers.Handle(ex, "Cannot create a note");
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      private UserDefinedSettings Settings { get; }
   }
}

