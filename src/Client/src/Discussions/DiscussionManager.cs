using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;
using mrHelper.Client.Discussions;
using mrHelper.Common.Exceptions;

namespace mrHelper.Client.Discussions
{
   public class DiscussionManagerException : Exception {}

   /// <summary>
   /// Manages merge request discussions
   /// </summary>
   public class DiscussionManager
   {
      public DiscussionManager(UserDefinedSettings settings)
      {
         DiscussionOperator = new DiscussionOperator(settings);
      }

      async public Task<List<Discussion>> GetDiscussionsAsync(MergeRequestKey mrk)
      {
         try
         {
            return await DiscussionOperator.GetDiscussionsAsync(mrk);
         }
         catch (OperatorException)
         {
            throw new DiscussionManagerException();
         }
      }

      public DiscussionCreator GetDiscussionCreator(MergeRequestKey mrk)
      {
         return new DiscussionCreator(mrk, DiscussionOperator);
      }

      public DiscussionEditor GetDiscussionEditor(MergeRequestKey mrk, string discussionId)
      {
         return new DiscussionEditor(mrk, discussionId, DiscussionOperator);
      }

      private DiscussionOperator DiscussionOperator { get; }
   }
}

