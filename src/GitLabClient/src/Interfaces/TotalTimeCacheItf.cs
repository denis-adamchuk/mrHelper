using System;
using System.Threading.Tasks;
using mrHelper.Common.Exceptions;

namespace mrHelper.GitLabClient
{
   public struct TrackedTime
   {
      public TrackedTime(TimeSpan? amount, EStatus status)
      {
         Amount = amount;
         Status = status;
      }

      public enum EStatus
      {
         NotAvailable,
         Loading,
         Ready
      }

      public TimeSpan? Amount { get; }
      public EStatus Status { get; }
   }

   public class TimeTrackingException : ExceptionEx
   {
      internal TimeTrackingException(string message, Exception innerException)
         : base(message, innerException)
      {
      }
   }

   public interface ITotalTimeCache : ITotalTimeLoader
   {
      TrackedTime GetTotalTime(MergeRequestKey mrk);
   }
}

