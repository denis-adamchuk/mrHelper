using System.Threading.Tasks;
using System.Collections.Generic;

namespace mrHelper.CustomActions
{
   public class CompositeCommand : ICommand
   {
      internal CompositeCommand(
         IEnumerable<ISubCommand> commands,
         string name,
         string enabledIf,
         string visibleIf,
         bool stopTimer,
         bool reload,
         string hint,
         bool initiallyVisible)
      {
         _commands = commands;
         Name = name;
         EnabledIf = enabledIf;
         VisibleIf = visibleIf;
         StopTimer = stopTimer;
         Reload = reload;
         Hint = hint;
         InitiallyVisible = initiallyVisible;
      }

      public string Name { get; }

      public string EnabledIf { get; }

      public string VisibleIf { get; }

      public bool StopTimer { get; }

      public bool Reload { get; }

      public string Hint { get; }

      public bool InitiallyVisible { get; }

      async public Task Run()
      {
         foreach (ISubCommand command in _commands)
         {
            await command.Run();
         }
      }

      private readonly IEnumerable<ISubCommand> _commands;
   }
}

