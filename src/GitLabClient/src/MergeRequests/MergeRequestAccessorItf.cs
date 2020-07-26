using System;
using System.Threading.Tasks;
using mrHelper.Common.Exceptions;
using GitLabSharp.Accessors;
using GitLabSharp.Entities;

namespace mrHelper.Client.MergeRequests
{
   public class MergeRequestAccessorException : ExceptionEx
   {
      internal MergeRequestAccessorException(string message, Exception innerException)
         : base(message, innerException)
      {
      }
   }

   public interface IMergeRequestAccessor
   {
      Task<MergeRequest> SearchMergeRequestAsync(int mergeRequestIId);

      IMergeRequestCreator GetMergeRequestCreator();

      ISingleMergeRequestAccessor GetSingleMergeRequestAccessor(int iid);
   }
}

