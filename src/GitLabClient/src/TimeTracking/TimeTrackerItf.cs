using System.Threading.Tasks;

namespace mrHelper.Client.TimeTracking
{
   public interface ITimeTracker
   {
      void Start();

      Task Stop();

      void Cancel();
   }
}

