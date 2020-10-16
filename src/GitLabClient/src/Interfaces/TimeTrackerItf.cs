using System;
using System.Threading.Tasks;
using mrHelper.Common.Exceptions;

namespace mrHelper.GitLabClient
{
   public class TimeTrackerException : ExceptionEx
   {
      internal TimeTrackerException(string message, Exception innerException)
         : base(message, innerException)
      {
      }
   }

   public class ForbiddenTimeTrackerException : TimeTrackerException
   {
      internal ForbiddenTimeTrackerException(Exception innerException)
         : base(String.Empty, innerException)
      {
      }
   }

   public interface ITimeTracker
   {
      void Start();

      Task Stop();

      void Cancel();

      TimeSpan Elapsed { get; }

      MergeRequestKey MergeRequest { get; }
   }
}

