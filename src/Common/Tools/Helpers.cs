using System;
using System.Diagnostics;

namespace mrHelper.Common.Tools
{
   public static class Helpers
   {
      /// <summary>
      /// Traverse process tree up to a process with the same name as the current process.
      /// Return process id of `git` process that is a child of a found process and parent of the current one.
      /// </summary>
      public static int GetGitParentProcessId(Process currentProcess)
      {
         Process previousParent = null;
         Process parent = ParentProcessUtilities.GetParentProcess(currentProcess);

         while (parent != null && parent.ProcessName != currentProcess.ProcessName)
         {
            previousParent = parent;
            parent = ParentProcessUtilities.GetParentProcess(parent);
         }

         if (previousParent == null || previousParent.ProcessName != "git")
         {
            return -1;
         }

         return previousParent.Id;
      }
   }
}

