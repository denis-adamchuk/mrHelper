namespace mrHelper.App.Forms
{
   internal class EditSearchQueryFormState
   {
      internal EditSearchQueryFormState(string projectName)
      {
         ProjectName = projectName;
      }

      internal EditSearchQueryFormState(
         bool isTitleAndDescriptionChecked, string titleAndDescription,
         bool isTargetBranchNameChecked, string targetBranchNameText,
         bool isProjectNameChecked, string projectName,
         bool isAuthorNameChecked, string authorUserName,
         string state)
      {
         IsTitleAndDescriptionChecked = isTitleAndDescriptionChecked;
         TitleAndDescriptionText = titleAndDescription;
         IsTargetBranchChecked = isTargetBranchNameChecked;
         TargetBranchNameText = targetBranchNameText;
         IsProjectChecked = isProjectNameChecked;
         ProjectName = projectName;
         IsAuthorNameChecked = isAuthorNameChecked;
         AuthorUserName = authorUserName;
         State = state;
      }

      internal bool IsTitleAndDescriptionChecked { get; }
      internal string TitleAndDescriptionText { get; }

      internal bool IsTargetBranchChecked { get; }
      internal string TargetBranchNameText { get; }

      internal bool IsProjectChecked { get; }
      internal string ProjectName { get; }

      internal bool IsAuthorNameChecked { get; }
      internal string AuthorUserName { get; }

      internal string State { get; }
   }
}

