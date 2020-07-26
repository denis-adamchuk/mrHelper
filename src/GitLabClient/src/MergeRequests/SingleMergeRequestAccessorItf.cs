using System;
using mrHelper.Client.Discussions;
using mrHelper.Common.Exceptions;

namespace mrHelper.Client.MergeRequests
{
   public class SingleMergeRequestAccessorException : ExceptionEx
   {
      internal SingleMergeRequestAccessorException(string message, Exception innerException)
         : base(message, innerException)
      {
      }
   }

   public interface ISingleMergeRequestAccessor
   {
      IMergeRequestEditor GetMergeRequestEditor();

      IDiscussionAccessor GetDiscussionAccessor();
   }
}

