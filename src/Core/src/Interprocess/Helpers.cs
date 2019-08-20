using System;
using System.Diagnostics;

namespace mrHelper.Core.Interprocess
{
   public static class Helpers
   {
      public static int GetGitParentProcessId(int mrHelperProcessId)
      {
         Process previousParent = null;
         Process parent = ParentProcessUtilities.GetParentProcess(mrHelperProcessId);

         while (parent != null && parent.ProcessName != "mrHelper")
         {
            previousParent = parent;
            parent = ParentProcessUtilities.GetParentProcess(parent.Id);
         }

         if (previousParent == null || previousParent.ProcessName != "git")
         {
            return -1;
         }

         return previousParent.Id;
      }
   }
}

