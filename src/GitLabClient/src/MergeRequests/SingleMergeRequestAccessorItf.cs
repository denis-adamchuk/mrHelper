using System;
using System.Threading.Tasks;
using mrHelper.Common.Exceptions;
using GitLabSharp.Accessors;
using mrHelper.Client.Discussions;
using GitLabSharp.Entities;

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
      IDiscussionCreator GetDiscussionCreator(User user);
   }
}

