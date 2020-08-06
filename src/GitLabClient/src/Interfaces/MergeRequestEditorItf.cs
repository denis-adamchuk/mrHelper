using System;
using System.Threading.Tasks;
using GitLabSharp.Accessors;
using GitLabSharp.Entities;
using mrHelper.Common.Exceptions;

namespace mrHelper.GitLabClient
{
   public class MergeRequestEditorException : ExceptionEx
   {
      public MergeRequestEditorException(string message, Exception innerException)
         : base(message, innerException)
      {
      }
   }

   public class MergeRequestEditorCancelledException : MergeRequestEditorException
   {
      public MergeRequestEditorCancelledException() : base(String.Empty, null)
      {
      }
   }

   public interface IMergeRequestEditor
   {
      Task<MergeRequest> ModifyMergeRequest(UpdateMergeRequestParameters parameters);

      Task AddTrackedTime(TimeSpan span, bool add);
   }
}

