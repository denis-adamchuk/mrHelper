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
         _operator = discussionOperator;
         _mergeRequestKey = mrk;
         _discussionId = discussionId;
      }

      async public Task<Discussion> GetDiscussion()
      {
         try
         {
            return await _operator.GetDiscussionAsync(_mergeRequestKey, _discussionId);
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
            await _operator.ReplyAsync(_mergeRequestKey, _discussionId, body);
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
            await _operator.ModifyNoteBodyAsync(_mergeRequestKey, _discussionId, noteId, body);
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
            await _operator.DeleteNoteAsync(_mergeRequestKey, noteId);
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
            await _operator.ResolveNoteAsync(_mergeRequestKey, _discussionId, noteId, resolved);
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
            return await _operator.ResolveDiscussionAsync(_mergeRequestKey, _discussionId, resolved);
         }
         catch (OperatorException)
         {
            throw new DiscussionEditorException();
         }
      }

      private readonly DiscussionOperator _operator;
      private MergeRequestKey _mergeRequestKey;
      private readonly string _discussionId;
   }
}

