using System;
using System.Threading.Tasks;
using GitLabSharp.Accessors;
using GitLabSharp.Entities;
using mrHelper.Common.Exceptions;

namespace mrHelper.GitLabClient
{
   public class MergeRequestCreatorException : ExceptionEx
   {
      public MergeRequestCreatorException(string message, Exception innerException)
         : base(message, innerException)
      {
      }
   }

   public class MergeRequestCreatorCancelledException : MergeRequestCreatorException
   {
      public MergeRequestCreatorCancelledException() : base(String.Empty, null)
      {
      }
   }

   public interface IMergeRequestCreator
   {
      Task<MergeRequest> CreateMergeRequest(CreateNewMergeRequestParameters parameters);
   }
}

