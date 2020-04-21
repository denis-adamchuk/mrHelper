using mrHelper.Client.Types;

namespace mrHelper.Client.Discussions
{
   public interface IDiscussionEditorFactory
   {
      DiscussionEditor GetDiscussionEditor(MergeRequestKey mrk, string discussionId);
   }
}

