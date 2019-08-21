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

      async public Task<List<Discussion>> GetDiscussionsAsync(MergeRequestDescriptor mrd)
      {
         try
         {
            return await DiscussionOperator.GetDiscussionsAsync(mrd);
         }
         catch (OperatorException)
         {
            throw new DiscussionManagerException();
         }
      }

      public DiscussionCreator GetDiscussionCreator(MergeRequestDescriptor mrd)
      {
         return new DiscussionCreator(mrd, DiscussionOperator);
      }

      public DiscussionEditor GetDiscussionEditor(MergeRequestDescriptor mrd, string discussionId)
      {
         return new DiscussionEditor(mrd, discussionId, DiscussionOperator);
      }

      private DiscussionOperator DiscussionOperator { get; }
   }
}

