using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrHelper.App.Forms.Helpers
{
   internal struct CreateNewMergeRequestState
   {
      public CreateNewMergeRequestState(string defaultProject, bool isSquashNeeded, bool isBranchDeletionNeeded)
      {
         DefaultProject = defaultProject;
         IsSquashNeeded = isSquashNeeded;
         IsBranchDeletionNeeded = isBranchDeletionNeeded;
      }

      internal string DefaultProject { get; }
      internal bool IsSquashNeeded { get; }
      internal bool IsBranchDeletionNeeded { get; }
   }
}

