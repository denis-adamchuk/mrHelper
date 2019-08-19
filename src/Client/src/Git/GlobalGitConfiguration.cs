using System;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.Git
{
   public class GlobalGitConfiguration : IGlobalGitConfiguration
   {
      /// <summary>
      /// Adds a difftool with the given name and command to the global git configuration.
      /// Throws GitOperationException in case of problems with git.
      /// </summary>
      public void SetGlobalDiffTool(string name, string command)
      {
         // No need to change current directory because we're changing a global setting
         string arguments = "config --global difftool." + name + ".cmd " + command;
         GitUtils.git(arguments);
      }

      /// <summary>
      /// Removes a section for the difftool with the passed name from the global git configuration.
      /// Throws GitOperationException in case of problems with git.
      /// </summary>
      public void RemoveGlobalDiffTool(string name)
      {
         // No need to change current directory because we're changing a global setting
         string arguments = "config --global --remove-section difftool." + name;
         GitUtils.git(arguments);
      }
   }
}

