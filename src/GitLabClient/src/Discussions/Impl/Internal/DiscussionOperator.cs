using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
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
   internal class DiscussionOperator : BaseOperator
   {
      internal DiscussionOperator(string hostname, IHostProperties settings)
         : base(hostname, settings)
      {
      }

      internal Task<Note> GetMostRecentUpdatedNoteAsync(MergeRequestKey mrk)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.CallNoCancel(
                  async () =>
                  ((IEnumerable<Note>)(await client.RunAsync(
                     async (gl) =>
                        await gl.Projects.Get(mrk.ProjectKey.ProjectName).MergeRequests.Get(mrk.IId).
                           Notes.LoadTaskAsync(new PageFilter(1, 1), new SortFilter(false, "updated_at")))))
                              .FirstOrDefault()));
      }

      internal Task<int> GetNoteCount(MergeRequestKey mrk)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.CallNoCancel(
                  async () =>
                     (int)(await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.Get(mrk.ProjectKey.ProjectName).MergeRequests.Get(mrk.IId).
                              Notes.CountAsync()))));
      }

      internal Task<IEnumerable<Discussion>> GetDiscussionsAsync(MergeRequestKey mrk)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.CallNoCancel(
                  async () =>
                     (IEnumerable<Discussion>)(await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.Get(mrk.ProjectKey.ProjectName).MergeRequests.Get(mrk.IId).
                              Discussions.LoadAllTaskAsync()))));
      }

      internal Task<Discussion> GetDiscussionAsync(MergeRequestKey mrk, string discussionId)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.CallNoCancel(
                  async () =>
                     (Discussion)(await client.RunAsync(
                        async (gitlab) =>
                           await gitlab.Projects.Get(mrk.ProjectKey.ProjectName).MergeRequests.Get(mrk.IId).
                              Discussions.Get(discussionId).LoadTaskAsync()))));
      }

      internal Task ReplyAsync(MergeRequestKey mrk, string discussionId, string body)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.CallNoCancel(
                  async () =>
                     await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.Get(mrk.ProjectKey.ProjectName).MergeRequests.Get(mrk.IId).
                              Discussions.Get(discussionId).CreateNewNoteTaskAsync(
                                 new CreateNewNoteParameters(body)))));
      }

      internal Task<DiscussionNote> ModifyNoteBodyAsync(MergeRequestKey mrk,
         string discussionId, int noteId, string body)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.CallNoCancel(
                  async () =>
                     (DiscussionNote)await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.Get(mrk.ProjectKey.ProjectName).MergeRequests.Get(mrk.IId).
                              Discussions.Get(discussionId).ModifyNoteTaskAsync(noteId,
                                 new ModifyDiscussionNoteParameters(
                                    ModifyDiscussionNoteParameters.ModificationType.Body, body, false)))));
      }

      internal Task DeleteNoteAsync(MergeRequestKey mrk, int noteId)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.CallNoCancel(
                  async () =>
                     await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.Get(mrk.ProjectKey.ProjectName).MergeRequests.Get(mrk.IId).
                              Notes.Get(noteId).DeleteTaskAsync())));
      }

      internal Task ResolveNoteAsync(MergeRequestKey mrk, string discussionId, int noteId, bool resolved)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.CallNoCancel(
                  async () =>
                     await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.Get(mrk.ProjectKey.ProjectName).MergeRequests.Get(mrk.IId).
                              Discussions.Get(discussionId).ModifyNoteTaskAsync(noteId,
                                 new ModifyDiscussionNoteParameters(
                                    ModifyDiscussionNoteParameters.ModificationType.Resolved, null, resolved)))));
      }

      internal Task<Discussion> ResolveDiscussionAsync(MergeRequestKey mrk, string discussionId, bool resolved)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.CallNoCancel(
                  async () =>
                     (Discussion)await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.Get(mrk.ProjectKey.ProjectName).MergeRequests.Get(mrk.IId).
                              Discussions.Get(discussionId).ResolveTaskAsync(
                                 new ResolveThreadParameters(resolved)))));
      }

      internal Task CreateDiscussionAsync(MergeRequestKey mrk, NewDiscussionParameters parameters)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.CallNoCancel(
                  async () =>
                     await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.Get(mrk.ProjectKey.ProjectName).MergeRequests.Get(mrk.IId).
                              Discussions.CreateNewTaskAsync(parameters))));
      }

      internal Task CreateNoteAsync(MergeRequestKey mrk, CreateNewNoteParameters parameters)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.CallNoCancel(
                  async () =>
                     await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.Get(mrk.ProjectKey.ProjectName).MergeRequests.Get(mrk.IId).
                              Notes.CreateNewTaskAsync(parameters))));
      }
   }
}

