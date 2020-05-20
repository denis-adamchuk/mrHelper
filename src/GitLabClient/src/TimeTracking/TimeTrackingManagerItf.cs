using System;
using System.Threading.Tasks;
using mrHelper.Client.Types;
using mrHelper.Common.Exceptions;

namespace mrHelper.Client.TimeTracking
{
   public class TimeTrackingException : ExceptionEx
   {
      internal TimeTrackingException(string message, Exception innerException)
         : base(message, innerException)
      {
      }
   }

   public interface ITotalTimeCache : ITotalTimeLoader
   {
      TimeSpan? GetTotalTime(MergeRequestKey mrk);

      Task AddSpan(bool add, TimeSpan span, MergeRequestKey mrk);
   }
}

