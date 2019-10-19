using System;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;
using mrHelper.Common.Exceptions;

namespace mrHelper.Client.Discussions
{
   public class DiscussionEditorException : Exception {}

   /// <summary>
   /// Implements logic of work with a single discussion
   /// </summary>
   public class DiscussionEditor
   {
      internal DiscussionEditor(MergeRequestKey mrk, string discussionId, DiscussionOperator discussionOperator)
      {
         DiscussionOperator = discussionOperator;
         MergeRequestKey = mrk;
         DiscussionId = discussionId;
      }

      async public Task<Discussion> GetDiscussion()
      {
         try
         {
            return await DiscussionOperator.GetDiscussionAsync(MergeRequestKey, DiscussionId);
         }
         catch (OperatorException)
         {
            throw new DiscussionEditorException();
         }
      }

      async public Task ReplyAsync(string body)
      {
         try
         {
            await DiscussionOperator.ReplyAsync(MergeRequestKey, DiscussionId, body);
         }
         catch (OperatorException)
         {
            throw new DiscussionEditorException();
         }
      }

      async public Task ModifyNoteBodyAsync(int noteId, string body)
      {
         try
         {
            await DiscussionOperator.ModifyNoteBodyAsync(MergeRequestKey, DiscussionId, noteId, body);
         }
         catch (OperatorException)
         {
            throw new DiscussionEditorException();
         }
      }

      async public Task DeleteNoteAsync(int noteId)
      {
         try
         {
            await DiscussionOperator.DeleteNoteAsync(MergeRequestKey, noteId);
         }
         catch (OperatorException)
         {
            throw new DiscussionEditorException();
         }
      }

      async public Task ResolveNoteAsync(int noteId, bool resolved)
      {
         try
         {
            await DiscussionOperator.ResolveNoteAsync(MergeRequestKey, DiscussionId, noteId, resolved);
         }
         catch (OperatorException)
         {
            throw new DiscussionEditorException();
         }
      }

      async public Task<Discussion> ResolveDiscussionAsync(bool resolved)
      {
         try
         {
            return await DiscussionOperator.ResolveDiscussionAsync(MergeRequestKey, DiscussionId, resolved);
         }
         catch (OperatorException)
         {
            throw new DiscussionEditorException();
         }
      }

      private DiscussionOperator DiscussionOperator { get; }
      private MergeRequestKey MergeRequestKey { get; }
      private string DiscussionId { get; }
   }
}

