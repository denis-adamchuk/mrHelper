using System;
using System.Threading.Tasks;
using mrHelper.Client.Types;

namespace mrHelper.Client.TimeTracking
{
   public interface ITimeTracker
   {
      void Start();

      Task Stop();

      void Cancel();

      TimeSpan Elapsed { get; }

      MergeRequestKey MergeRequest { get; }
   }
}

