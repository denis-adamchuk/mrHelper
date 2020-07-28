using System;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient.Operators;

namespace mrHelper.GitLabClient.Accessors
{
   /// <summary>
   /// Implements logic of work with a single discussion
   /// </summary>
   internal class DiscussionEditor : IDiscussionEditor
   {
      internal DiscussionEditor(MergeRequestKey mrk, string discussionId, IHostProperties hostProperties,
         IModificationListener modificationListener)
      {
         _operator = new DiscussionOperator(mrk.ProjectKey.HostName, hostProperties);
         _mergeRequestKey = mrk;
         _discussionId = discussionId;
         _modificationListener = modificationListener;
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

      async public Task ReplyAndResolveDiscussionAsync(string body, bool resolve)
      {
         try
         {
            await _operator.ReplyAndResolveDiscussionAsync(_mergeRequestKey, _discussionId, body, resolve);
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

      async public Task ResolveNoteAsync(int noteId, bool resolve)
      {
         try
         {
            await _operator.ResolveNoteAsync(_mergeRequestKey, _discussionId, noteId, resolve);
         }
         catch (OperatorException ex)
         {
            throw new DiscussionEditorException("Cannot change discussion note resolve state", ex);
         }
      }

      async public Task<Discussion> ResolveDiscussionAsync(bool resolve)
      {
         try
         {
            return await _operator.ResolveDiscussionAsync(_mergeRequestKey, _discussionId, resolve);
         }
         catch (OperatorException ex)
         {
            throw new DiscussionEditorException("Cannot change discussion resolve state", ex);
         }
         finally
         {
            _modificationListener.OnDiscussionResolved(_mergeRequestKey);
         }
      }

      private readonly DiscussionOperator _operator;
      private readonly MergeRequestKey _mergeRequestKey;
      private readonly string _discussionId;
      private readonly IModificationListener _modificationListener;
   }
}

