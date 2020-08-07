namespace mrHelper.App.Forms.Helpers
{
   internal struct NewMergeRequestProperties
   {
      public NewMergeRequestProperties(string defaultProject, string sourceBranch, string targetBranch,
         string assigneeUsername, bool isSquashNeeded, bool isBranchDeletionNeeded)
      {
         DefaultProject = defaultProject;
         SourceBranch = sourceBranch;
         TargetBranch = targetBranch;
         AssigneeUsername = assigneeUsername;
         IsSquashNeeded = isSquashNeeded;
         IsBranchDeletionNeeded = isBranchDeletionNeeded;
      }

      internal string DefaultProject { get; }
      internal string SourceBranch { get; }
      internal string TargetBranch { get; }
      internal string AssigneeUsername { get; }
      internal bool IsSquashNeeded { get; }
      internal bool IsBranchDeletionNeeded { get; }
   }
}

