using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace mrHelper.CustomActions
{
   public class CompositeCommand : ICommand, ICommandCallback
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

      async public Task Run(ICommandCallback callback)
      {
         if (_isRunning)
         {
            return;
         }

         _isRunning = true;
         _currentHostName = callback.GetCurrentHostName();
         _currentAccessToken = callback.GetCurrentAccessToken();
         _currentProjectName = callback.GetCurrentProjectName();
         _currentMergeRequestIId = callback.GetCurrentMergeRequestIId();
         try
         {
            foreach (ISubCommand command in _commands)
            {
               await command.Run(this);
               if (command != _commands.Last())
               {
                  await Task.Delay(SubCommandDelay);
               }
            }
         }
         finally
         {
            _currentHostName = callback.GetCurrentHostName();
            _currentAccessToken = callback.GetCurrentAccessToken();
            _currentProjectName = callback.GetCurrentProjectName();
            _currentMergeRequestIId = callback.GetCurrentMergeRequestIId();
            _isRunning = false;
         }
      }

      public string GetCurrentHostName() => _currentHostName;
      public string GetCurrentAccessToken() => _currentAccessToken;
      public string GetCurrentProjectName() => _currentProjectName;
      public int GetCurrentMergeRequestIId() => _currentMergeRequestIId;

      private readonly int SubCommandDelay = 250; // 0.25s

      private readonly IEnumerable<ISubCommand> _commands;
      private string _currentHostName;
      private string _currentAccessToken;
      private string _currentProjectName;
      private int _currentMergeRequestIId;
      private bool _isRunning;
   }
}

