using System.Collections.Generic;

namespace mrHelper.App.Forms.Helpers
{
   public struct NewMergeRequestProperties
   {
      public NewMergeRequestProperties(string defaultProject, string sourceBranch,
         IEnumerable<string> targetBranchCandidates, string assigneeUsername,
         bool isSquashNeeded, bool isBranchDeletionNeeded,
         string[] favoriteProjects)
      {
         DefaultProject = defaultProject;
         SourceBranch = sourceBranch;
         TargetBranchCandidates = targetBranchCandidates;
         AssigneeUsername = assigneeUsername;
         IsSquashNeeded = isSquashNeeded;
         IsBranchDeletionNeeded = isBranchDeletionNeeded;
         FavoriteProjects = favoriteProjects;
      }

      internal string DefaultProject { get; }
      internal string SourceBranch { get; }
      internal IEnumerable<string> TargetBranchCandidates { get; }
      internal string AssigneeUsername { get; }
      internal bool IsSquashNeeded { get; }
      internal bool IsBranchDeletionNeeded { get; }
      internal string[] FavoriteProjects { get; }
   }
}

