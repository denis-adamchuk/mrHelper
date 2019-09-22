using System;
using System.Diagnostics;

namespace mrHelper.Common.Tools
{
   public static class Helpers
   {
      public static int GetGitParentProcessId(Process currentProcess, Process mainInstance)
      {
         Process previousParent = null;
         Process parent = ParentProcessUtilities.GetParentProcess(currentProcess);

         while (parent != null && parent != mainInstance)
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

