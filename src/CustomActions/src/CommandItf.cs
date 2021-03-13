using System.Threading.Tasks;

namespace mrHelper.CustomActions
{
   internal interface ISubCommand
   {
      Task Run();
   }

   public interface ICommand
   {
      string Name { get; }

      string EnabledIf { get; }

      string VisibleIf { get; }

      bool StopTimer { get; }

      bool Reload { get; }

      string Hint { get; }

      bool InitiallyVisible { get; }

      Task Run();
   }
}
