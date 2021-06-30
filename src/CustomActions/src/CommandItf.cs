using System.Threading.Tasks;

namespace mrHelper.CustomActions
{
   internal interface ISubCommand
   {
      Task Run(ICommandCallback callback);
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

      bool ShowInDiscussionsMenu { get; }

      Task Run(ICommandCallback callback);
   }
}

