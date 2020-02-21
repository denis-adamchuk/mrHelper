using System;
using System.Threading.Tasks;

namespace mrHelper.Common.Interfaces
{
   public interface IInstantProjectChecker
   {
      Task<DateTime> GetLatestChangeTimestamp();
   }
}

