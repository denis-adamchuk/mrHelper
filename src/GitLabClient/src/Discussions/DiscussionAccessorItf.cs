using System;
using mrHelper.Common.Exceptions;
using GitLabSharp.Entities;

namespace mrHelper.Client.Discussions
{
   public class DiscussionAccessorException : ExceptionEx
   {
      internal DiscussionAccessorException(string message, Exception innerException)
         : base(message, innerException)
      {
      }
   }

   public interface IDiscussionAccessor
   {
      IDiscussionCreator GetDiscussionCreator(User user);

      ISingleDiscussionAccessor GetSingleDiscussionAccessor(string discussionId);
   }
}

