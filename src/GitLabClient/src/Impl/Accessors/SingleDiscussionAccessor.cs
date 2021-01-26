﻿using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient.Accessors;
using mrHelper.GitLabClient.Interfaces;
using mrHelper.GitLabClient.Operators;
using System.Threading.Tasks;

namespace mrHelper.GitLabClient
{
   public class SingleDiscussionAccessor
   {
      internal SingleDiscussionAccessor(IHostProperties settings, MergeRequestKey mrk, string discussionId,
         IModificationListener modificationListener, INetworkOperationStatusListener networkOperationStatusListener)
      {
         _settings = settings;
         _mrk = mrk;
         _discussionId = discussionId;
         _modificationListener = modificationListener;
         _networkOperationStatusListener = networkOperationStatusListener;
      }

      async public Task<Discussion> GetDiscussion()
      {
         using (DiscussionOperator discussionOperator = new DiscussionOperator(
            _mrk.ProjectKey.HostName, _settings, _networkOperationStatusListener))
         {
            try
            {
               return await discussionOperator.GetDiscussionAsync(_mrk, _discussionId);
            }
            catch (OperatorException ex)
            {
               if (ex.InnerException is GitLabSharp.Accessors.GitLabRequestException rx)
               {
                  if (rx.InnerException is System.Net.WebException wx)
                  {
                     if (wx.Response is System.Net.HttpWebResponse response
                      && response.StatusCode == System.Net.HttpStatusCode.NotFound)
                     {
                        // it is not an error here, we treat it as 'last discussion item has been deleted'
                        return null;
                     }
                  }
               }
               throw new DiscussionEditorException("Cannot obtain discussion", ex);
            }
         }
      }

      public IDiscussionEditor GetDiscussionEditor()
      {
         return new DiscussionEditor(_mrk, _discussionId, _settings, _modificationListener, _networkOperationStatusListener);
      }

      private readonly IHostProperties _settings;
      private readonly MergeRequestKey _mrk;
      private readonly string _discussionId;
      private readonly IModificationListener _modificationListener;
      private readonly INetworkOperationStatusListener _networkOperationStatusListener;
   }
}

