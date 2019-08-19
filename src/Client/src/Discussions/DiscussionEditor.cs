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
      internal DiscussionEditor(MergeRequestDescriptor mrd, string discussionId, DiscussionOperator discussionOperator)
      {
         DiscussionOperator = discussionOperator;
         MergeRequestDescriptor = mrd;
         DiscussionId = discussionId;
      }

      async public Task<Discussion> GetDiscussion()
      {
         try
         {
            return await DiscussionOperator.GetDiscussionAsync(MergeRequestDescriptor, DiscussionId);
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
            await DiscussionOperator.ReplyAsync(MergeRequestDescriptor, DiscussionId, body);
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
            await DiscussionOperator.ModifyNoteBodyAsync(MergeRequestDescriptor, DiscussionId, noteId, body);
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
            await DiscussionOperator.DeleteNoteAsync(MergeRequestDescriptor, noteId);
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
            await DiscussionOperator.ResolveNoteAsync(MergeRequestDescriptor, DiscussionId, noteId, resolved);
         }
         catch (OperatorException)
         {
            throw new DiscussionEditorException();
         }
      }

      async public Task ResolveDiscussionAsync(bool resolved)
      {
         try
         {
            await DiscussionOperator.ResolveDiscussionAsync(MergeRequestDescriptor, DiscussionId, resolved);
         }
         catch (OperatorException)
         {
            throw new DiscussionEditorException();
         }
      }

      private DiscussionOperator DiscussionOperator { get; }
      private MergeRequestDescriptor MergeRequestDescriptor { get; }
      private string DiscussionId { get; }
   }
}

