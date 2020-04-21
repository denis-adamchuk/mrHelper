using mrHelper.Client.Types;

namespace mrHelper.Client.Discussions
{
   public interface IDiscussionCreatorFactory
   {
      DiscussionCreator GetDiscussionCreator(MergeRequestKey mrk);
   }
}

