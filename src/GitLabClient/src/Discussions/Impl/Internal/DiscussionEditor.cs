using System;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Client.Common;
using mrHelper.Client.Types;
using mrHelper.Common.Exceptions;

namespace mrHelper.Client.Discussions
{
   public class DiscussionEditorException : ExceptionEx
   {
      internal DiscussionEditorException(string message, Exception innerException)
         : base(message, innerException)
      {
      }
   }

   /// <summary>
   /// Implements logic of work with a single discussion
   /// </summary>
   internal class DiscussionEditor : IDiscussionEditor
   {
      internal DiscussionEditor(MergeRequestKey mrk, string discussionId, DiscussionOperator discussionOperator,
         Action onDiscussionResolved)
      {
         _operator = discussionOperator;
         _mergeRequestKey = mrk;
         _discussionId = discussionId;
         _onDiscussionResolved = onDiscussionResolved;
      }

      async public Task<Discussion> GetDiscussion()
      {
         try
         {
            return await _operator.GetDiscussionAsync(_mergeRequestKey, _discussionId);
         }
         catch (OperatorException ex)
         {
            throw new DiscussionEditorException("Cannot obtain discussion", ex);
         }
      }

      async public Task ReplyAsync(string body)
      {
         try
         {
            await _operator.ReplyAsync(_mergeRequestKey, _discussionId, body);
         }
         catch (OperatorException ex)
         {
            throw new DiscussionEditorException("Cannot send reply", ex);
         }
      }

      async public Task<DiscussionNote> ModifyNoteBodyAsync(int noteId, string body)
      {
         try
         {
            return await _operator.ModifyNoteBodyAsync(_mergeRequestKey, _discussionId, noteId, body);
         }
         catch (OperatorException ex)
         {
            throw new DiscussionEditorException("Cannot modify discussion body", ex);
         }
      }

      async public Task DeleteNoteAsync(int noteId)
      {
         try
         {
            await _operator.DeleteNoteAsync(_mergeRequestKey, noteId);
         }
         catch (OperatorException ex)
         {
            throw new DiscussionEditorException("Cannot delete discussion note", ex);
         }
      }

      async public Task ResolveNoteAsync(int noteId, bool resolved)
      {
         try
         {
            await _operator.ResolveNoteAsync(_mergeRequestKey, _discussionId, noteId, resolved);
         }
         catch (OperatorException ex)
         {
            throw new DiscussionEditorException("Cannot change discussion note resolve state", ex);
         }
      }

      async public Task<Discussion> ResolveDiscussionAsync(bool resolved)
      {
         try
         {
            return await _operator.ResolveDiscussionAsync(_mergeRequestKey, _discussionId, resolved);
         }
         catch (OperatorException ex)
         {
            throw new DiscussionEditorException("Cannot change discussion resolve state", ex);
         }
         finally
         {
            _onDiscussionResolved();
         }
      }

      private readonly DiscussionOperator _operator;
      private readonly MergeRequestKey _mergeRequestKey;
      private readonly string _discussionId;
      private readonly Action _onDiscussionResolved;
   }
}

