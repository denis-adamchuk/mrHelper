using System;
using System.Threading.Tasks;
using mrHelper.Common.Exceptions;

namespace mrHelper.GitLabClient
{
   public class TimeTrackerException : ExceptionEx
   {
      internal TimeTrackerException(string message, Exception innerException, TimeSpan trackedTime)
         : base(message, innerException)
      {
         TrackedTime = trackedTime;
      }

      public TimeSpan TrackedTime { get; }
   }

   public class ForbiddenTimeTrackerException : TimeTrackerException
   {
      internal ForbiddenTimeTrackerException(Exception innerException, TimeSpan trackedTime)
         : base(String.Empty, innerException, trackedTime)
      {
      }
   }

   public class TooSmallSpanTimeTrackerException : TimeTrackerException
   {
      internal TooSmallSpanTimeTrackerException(Exception innerException, TimeSpan trackedTime)
         : base(String.Empty, innerException, trackedTime)
      {
      }
   }

   public interface ITimeTracker
   {
      void Start();

      Task<TimeSpan> Stop();

      void Cancel();

      TimeSpan Elapsed { get; }

      MergeRequestKey MergeRequest { get; }
   }
}

