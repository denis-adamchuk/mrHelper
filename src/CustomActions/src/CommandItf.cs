using System.Threading.Tasks;

namespace mrHelper.CustomActions
{
   public interface ICommand
   {
      string GetName();

      string GetEnabledIf();

      string GetVisibleIf();

      bool GetStopTimer();

      bool GetReload();

      string GetHint();

      Task Run();
   }
}
