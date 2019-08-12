using System;

namespace mrHelper.Client
{
   public class DiscussionManagerException : Exception {}

   /// <summary>
   /// Manages merge request discussions
   /// </summary>
   public class DiscussionManager
   {
      public DiscussionManager(UserDefinedSettings settings)
      {
         Settings = settings;
         DiscussionOperator = new DiscussionOperator(settings);
      }

      async public Task<List<Discussion>> GetDiscussionsAsync(MergeRequestDescriptor mrd)
      {
         try
         {
            await DiscussionOperator.GetDiscussionsAsync(mrd);
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

      private Settings Settings { get; }
      private DiscussionOperator DiscussionOperator { get; }
   }
}

