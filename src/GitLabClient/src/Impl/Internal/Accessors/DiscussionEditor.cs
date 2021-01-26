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
         IModificationListener modificationListener, INetworkOperationStatusListener networkOperationStatusListener)
      {
         _hostProperties = hostProperties;
         _mergeRequestKey = mrk;
         _discussionId = discussionId;
         _modificationListener = modificationListener;
         _networkOperationStatusListener = networkOperationStatusListener;
      }

      async public Task ReplyAsync(string body)
      {
         using (DiscussionOperator discussionOperator = new DiscussionOperator(_mergeRequestKey.ProjectKey.HostName,
               _hostProperties, _networkOperationStatusListener))
         {
            try
            {
               await discussionOperator.ReplyAsync(_mergeRequestKey, _discussionId, body);
            }
            catch (OperatorException ex)
            {
               throw new DiscussionEditorException("Cannot send reply", ex);
            }
         }
      }

      async public Task ReplyAndResolveDiscussionAsync(string body, bool resolve)
      {
         using (DiscussionOperator discussionOperator = new DiscussionOperator(_mergeRequestKey.ProjectKey.HostName,
               _hostProperties, _networkOperationStatusListener))
         {
            try
            {
               await discussionOperator.ReplyAndResolveDiscussionAsync(_mergeRequestKey, _discussionId, body, resolve);
            }
            catch (OperatorException ex)
            {
               throw new DiscussionEditorException("Cannot send reply", ex);
            }
         }
      }

      async public Task<DiscussionNote> ModifyNoteBodyAsync(int noteId, string body)
      {
         using (DiscussionOperator discussionOperator = new DiscussionOperator(_mergeRequestKey.ProjectKey.HostName,
               _hostProperties, _networkOperationStatusListener))
         {
            try
            {
               return await discussionOperator.ModifyNoteBodyAsync(_mergeRequestKey, _discussionId, noteId, body);
            }
            catch (OperatorException ex)
            {
               throw new DiscussionEditorException("Cannot modify discussion body", ex);
            }
         }
      }

      async public Task DeleteNoteAsync(int noteId)
      {
         using (DiscussionOperator discussionOperator = new DiscussionOperator(_mergeRequestKey.ProjectKey.HostName,
               _hostProperties, _networkOperationStatusListener))
         {
            try
            {
               await discussionOperator.DeleteNoteAsync(_mergeRequestKey, noteId);
            }
            catch (OperatorException ex)
            {
               throw new DiscussionEditorException("Cannot delete discussion note", ex);
            }
         }
      }

      async public Task ResolveNoteAsync(int noteId, bool resolve)
      {
         using (DiscussionOperator discussionOperator = new DiscussionOperator(_mergeRequestKey.ProjectKey.HostName,
               _hostProperties, _networkOperationStatusListener))
         {
            try
            {
               await discussionOperator.ResolveNoteAsync(_mergeRequestKey, _discussionId, noteId, resolve);
            }
            catch (OperatorException ex)
            {
               throw new DiscussionEditorException("Cannot change discussion note resolve state", ex);
            }
         }
      }

      async public Task<Discussion> ResolveDiscussionAsync(bool resolve)
      {
         using (DiscussionOperator discussionOperator = new DiscussionOperator(_mergeRequestKey.ProjectKey.HostName,
               _hostProperties, _networkOperationStatusListener))
         {
            try
            {
               return await discussionOperator.ResolveDiscussionAsync(_mergeRequestKey, _discussionId, resolve);
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
      }

      private readonly MergeRequestKey _mergeRequestKey;
      private readonly IHostProperties _hostProperties;
      private readonly string _discussionId;
      private readonly IModificationListener _modificationListener;
      private readonly INetworkOperationStatusListener _networkOperationStatusListener;
   }
}

