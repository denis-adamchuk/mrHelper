using System;
using System.Threading.Tasks;

namespace mrHelper.Client.MergeRequests
{
   public interface IInstantProjectChecker
   {
      Task<DateTime> GetLatestChangeTimestampAsync();
   }
}

