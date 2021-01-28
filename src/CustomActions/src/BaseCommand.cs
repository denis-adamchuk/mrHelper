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
         string hint)
      {
         _callback = callback;
         _name = name;
         _enabledIf = enabledIf;
         _visibleIf = visibleIf;
         _stopTimer = stopTimer;
         _reload = reload;
         _hint = hint;
      }

      public string GetName()
      {
         return _name;
      }

      public string GetEnabledIf()
      {
         return _enabledIf;
      }

      public string GetVisibleIf()
      {
         return _visibleIf;
      }

      public bool GetStopTimer()
      {
         return _stopTimer;
      }

      public bool GetReload()
      {
         return _reload;
      }

      public string GetHint()
      {
         return _hint;
      }

      public abstract Task Run();

      protected readonly ICommandCallback _callback;
      private readonly string _name;
      private readonly string _enabledIf;
      private readonly string _visibleIf;
      private readonly bool _stopTimer;
      private readonly bool _reload;
      private readonly string _hint;
   }
}

