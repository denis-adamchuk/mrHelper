using System.Threading.Tasks;

namespace mrHelper.CustomActions
{
   public interface ICommand
   {
      string Name { get; }

      string EnabledIf { get; }

      string VisibleIf { get; }

      bool StopTimer { get; }

      bool Reload { get; }

      string Hint { get; }

      Task Run();
   }
}
