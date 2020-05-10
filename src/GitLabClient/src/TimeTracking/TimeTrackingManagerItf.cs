using System;
using System.Threading.Tasks;
using mrHelper.Client.Common;
using mrHelper.Client.Types;
using mrHelper.Common.Exceptions;

namespace mrHelper.Client.TimeTracking
{
   public class TimeTrackingManagerException : ExceptionEx
   {
      internal TimeTrackingManagerException(string message, Exception innerException)
         : base(message, innerException)
      {
      }
   }

   public interface ITimeTrackingManager : ILoader<ITimeTrackingLoaderListener>
   {
      TimeSpan? GetTotalTime(MergeRequestKey mrk);

      Task AddSpanAsync(bool add, TimeSpan span, MergeRequestKey mrk);

      TimeTracker GetTracker(MergeRequestKey mrk);
   }
}

