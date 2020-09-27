using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp.Accessors;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;

namespace mrHelper.GitLabClient.Operators
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

      internal Task<Tuple<Note, int>> GetMostRecentUpdatedNoteAndCountAsync(MergeRequestKey mrk)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
               {
                  Tuple<IEnumerable<Note>, int> notesAndCount = (Tuple<IEnumerable<Note>, int>)(await client.RunAsync(
                     async (gl) =>
                        await gl.Projects.Get(mrk.ProjectKey.ProjectName).MergeRequests.Get(mrk.IId).
                           Notes.LoadAndCalculateTotalCountAsync(new PageFilter(1, 1),
                              new SortFilter(false, "updated_at"))));
                  return new Tuple<Note, int>(notesAndCount.Item1.FirstOrDefault(), notesAndCount.Item2);
               }));
      }

      internal Task<IEnumerable<Discussion>> GetDiscussionsAsync(MergeRequestKey mrk)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
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
               await OperatorCallWrapper.Call(
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
               await OperatorCallWrapper.Call(
                  async () =>
                     await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.Get(mrk.ProjectKey.ProjectName).MergeRequests.Get(mrk.IId).
                              Discussions.Get(discussionId).CreateNewNoteTaskAsync(
                                 new CreateNewNoteParameters(body)))));
      }

      internal Task ReplyAndResolveDiscussionAsync(MergeRequestKey mrk, string discussionId, string body, bool resolve)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     await client.RunAsync(
                        async (gl) =>
                        {
                           GitLabSharp.Accessors.SingleProjectAccessor projectAccessor =
                              gl.Projects.Get(mrk.ProjectKey.ProjectName);
                           GitLabSharp.Accessors.SingleMergeRequestAccessor mrAccessor =
                              projectAccessor.MergeRequests.Get(mrk.IId);
                           GitLabSharp.Accessors.SingleDiscussionAccessor accessor =
                              mrAccessor.Discussions.Get(discussionId);
                           await accessor.CreateNewNoteTaskAsync(new CreateNewNoteParameters(body));
                           await accessor.ResolveTaskAsync(new ResolveThreadParameters(resolve));
                           return true;
                        })));
      }

      internal Task<DiscussionNote> ModifyNoteBodyAsync(MergeRequestKey mrk,
         string discussionId, int noteId, string body)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
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
               await OperatorCallWrapper.Call(
                  async () =>
                     await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.Get(mrk.ProjectKey.ProjectName).MergeRequests.Get(mrk.IId).
                              Notes.Get(noteId).DeleteTaskAsync())));
      }

      internal Task ResolveNoteAsync(MergeRequestKey mrk, string discussionId, int noteId, bool resolve)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.Get(mrk.ProjectKey.ProjectName).MergeRequests.Get(mrk.IId).
                              Discussions.Get(discussionId).ModifyNoteTaskAsync(noteId,
                                 new ModifyDiscussionNoteParameters(
                                    ModifyDiscussionNoteParameters.ModificationType.Resolved, null, resolve)))));
      }

      internal Task<Discussion> ResolveDiscussionAsync(MergeRequestKey mrk, string discussionId, bool resolve)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     (Discussion)await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.Get(mrk.ProjectKey.ProjectName).MergeRequests.Get(mrk.IId).
                              Discussions.Get(discussionId).ResolveTaskAsync(
                                 new ResolveThreadParameters(resolve)))));
      }

      internal Task<Discussion> CreateDiscussionAsync(MergeRequestKey mrk, NewDiscussionParameters parameters)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     (Discussion)await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.Get(mrk.ProjectKey.ProjectName).MergeRequests.Get(mrk.IId).
                              Discussions.CreateNewTaskAsync(parameters))));
      }

      internal Task CreateNoteAsync(MergeRequestKey mrk, CreateNewNoteParameters parameters)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.Get(mrk.ProjectKey.ProjectName).MergeRequests.Get(mrk.IId).
                              Notes.CreateNewTaskAsync(parameters))));
      }
   }
}

