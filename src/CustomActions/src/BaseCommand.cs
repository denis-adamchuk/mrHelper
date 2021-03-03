using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrHelper.CustomActions
{
   public abstract class BaseCommand : ICommand
   {
      public BaseCommand(
         ICommandCallback callback,
         string name,
         string enabledIf,
         string visibleIf,
         bool stopTimer,
         bool reload,
         string hint,
         bool initiallyVisible)
      {
         _callback = callback;
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

      public abstract Task Run();

      protected readonly ICommandCallback _callback;
   }
}

