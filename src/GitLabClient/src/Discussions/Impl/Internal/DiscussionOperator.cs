using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp;
using GitLabSharp.Accessors;
using GitLabSharp.Entities;
using mrHelper.Client.Types;
using mrHelper.Client.Common;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.Discussions
{
   /// <summary>
   /// Implements Discussions-related interaction with GitLab
   /// </summary>
   internal class DiscussionOperator
   {
      internal DiscussionOperator(IHostProperties settings)
      {
         _settings = settings;
      }

      async internal Task<Note> GetMostRecentUpdatedNoteAsync(MergeRequestKey mrk)
      {
         GitLabClient client = new GitLabClient(mrk.ProjectKey.HostName,
            _settings.GetAccessToken(mrk.ProjectKey.HostName));
         try
         {
            IEnumerable<Note> notes = (IEnumerable<Note>)(await client.RunAsync(async (gitlab) =>
               await gitlab.Projects.Get(mrk.ProjectKey.ProjectName).MergeRequests.Get(mrk.IId).
                  Notes.LoadTaskAsync(new PageFilter { PerPage = 1, PageNumber = 1 },
                                      new SortFilter { Ascending = false, OrderBy = "updated_at" })));
            return notes.Any() ? notes.First() : new Note();
         }
         catch (Exception ex)
         {
            if (ex is GitLabSharpException || ex is GitLabRequestException || ex is GitLabClientCancelled)
            {
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      async internal Task<int> GetNoteCount(MergeRequestKey mrk)
      {
         GitLabClient client = new GitLabClient(mrk.ProjectKey.HostName,
            _settings.GetAccessToken(mrk.ProjectKey.HostName));
         try
         {
            return (int)(await client.RunAsync(async (gitlab) =>
               await gitlab.Projects.Get(mrk.ProjectKey.ProjectName).MergeRequests.Get(mrk.IId).
                  Notes.CountAsync()));
         }
         catch (Exception ex)
         {
            Debug.Assert(!(ex is GitLabClientCancelled));
            if (ex is GitLabSharpException || ex is GitLabRequestException)
            {
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      async internal Task<IEnumerable<Discussion>> GetDiscussionsAsync(MergeRequestKey mrk)
      {
         GitLabClient client = new GitLabClient(mrk.ProjectKey.HostName,
            _settings.GetAccessToken(mrk.ProjectKey.HostName));
         try
         {
            return (IEnumerable<Discussion>)(await client.RunAsync(async (gitlab) =>
               await gitlab.Projects.Get(mrk.ProjectKey.ProjectName).MergeRequests.Get(mrk.IId).
                  Discussions.LoadAllTaskAsync()));
         }
         catch (Exception ex)
         {
            Debug.Assert(!(ex is GitLabClientCancelled));
            if (ex is GitLabSharpException || ex is GitLabRequestException)
            {
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      async internal Task<Discussion> GetDiscussionAsync(MergeRequestKey mrk, string discussionId)
      {
         GitLabClient client = new GitLabClient(mrk.ProjectKey.HostName,
            _settings.GetAccessToken(mrk.ProjectKey.HostName));
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
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      async internal Task ReplyAsync(MergeRequestKey mrk, string discussionId, string body)
      {
         GitLabClient client = new GitLabClient(mrk.ProjectKey.HostName,
            _settings.GetAccessToken(mrk.ProjectKey.HostName));
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
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      async internal Task<DiscussionNote> ModifyNoteBodyAsync(MergeRequestKey mrk, string discussionId, int noteId, string body)
      {
         GitLabClient client = new GitLabClient(mrk.ProjectKey.HostName,
            _settings.GetAccessToken(mrk.ProjectKey.HostName));
         try
         {
            return (DiscussionNote)await client.RunAsync(async (gitlab) =>
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
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      async internal Task DeleteNoteAsync(MergeRequestKey mrk, int noteId)
      {
         GitLabClient client = new GitLabClient(mrk.ProjectKey.HostName,
            _settings.GetAccessToken(mrk.ProjectKey.HostName));
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
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      async internal Task ResolveNoteAsync(MergeRequestKey mrk, string discussionId, int noteId, bool resolved)
      {
         GitLabClient client = new GitLabClient(mrk.ProjectKey.HostName,
            _settings.GetAccessToken(mrk.ProjectKey.HostName));
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
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      async internal Task<Discussion> ResolveDiscussionAsync(MergeRequestKey mrk, string discussionId, bool resolved)
      {
         GitLabClient client = new GitLabClient(mrk.ProjectKey.HostName,
            _settings.GetAccessToken(mrk.ProjectKey.HostName));
         try
         {
            return (Discussion)await client.RunAsync(async (gitlab) =>
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
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      async internal Task CreateDiscussionAsync(MergeRequestKey mrk, NewDiscussionParameters parameters)
      {
         GitLabClient client = new GitLabClient(mrk.ProjectKey.HostName,
            _settings.GetAccessToken(mrk.ProjectKey.HostName));
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
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      async internal Task CreateNoteAsync(MergeRequestKey mrk, CreateNewNoteParameters parameters)
      {
         GitLabClient client = new GitLabClient(mrk.ProjectKey.HostName,
            _settings.GetAccessToken(mrk.ProjectKey.HostName));
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
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      private readonly IHostProperties _settings;
   }
}

