using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.Common.Constants;
using mrHelper.CommonControls.Tools;
using mrHelper.GitLabClient;

namespace mrHelper.App.Forms
{
   internal partial class EditSearchQueryForm : CustomFontForm
   {
      public EditSearchQueryForm(IEnumerable<string> projectNames, IEnumerable<User> users,
         User currentUser, EditSearchQueryFormState initialState)
      {
         CommonControls.Tools.WinFormsHelpers.FixNonStandardDPIIssue(this,
            (float)Common.Constants.Constants.FontSizeChoices["Design"]);
         InitializeComponent();
         applyFont(Program.Settings.MainWindowFontSizeName);
         setTooltipsForSearchOptions();

         _currentUser = currentUser ?? throw new ArgumentException("currentUser cannot be null");
         fillProjectList(projectNames);
         fillUserList(users);
         applyInitialState(initialState);
      }

      internal SearchQuery SearchQuery
      {
         get
         {
            SearchQuery query = new SearchQuery
            {
               MaxResults = Constants.MaxSearchResults
            };

            if (checkBoxSearchByTargetBranch.Checked && !String.IsNullOrWhiteSpace(textBoxSearchTargetBranch.Text))
            {
               query.TargetBranchName = textBoxSearchTargetBranch.Text;
            }
            if (checkBoxSearchByTitleAndDescription.Checked && !String.IsNullOrWhiteSpace(textBoxSearchTitleAndDescription.Text))
            {
               query.Text = textBoxSearchTitleAndDescription.Text;
            }
            if (checkBoxSearchByProject.Checked && comboBoxProjectName.SelectedItem != null)
            {
               query.ProjectName = comboBoxProjectName.Text;
            }
            if (checkBoxSearchByAuthor.Checked && comboBoxUser.SelectedItem != null)
            {
               query.AuthorUserName = (comboBoxUser.SelectedItem as User).Username;
            }

            query.State = getStateAsText();

            return query;
         }
      }

      internal EditSearchQueryFormState State
      {
         get
         {
            return new EditSearchQueryFormState(
               checkBoxSearchByTitleAndDescription.Checked,
               textBoxSearchTitleAndDescription.Text,
               checkBoxSearchByTargetBranch.Checked,
               textBoxSearchTargetBranch.Text,
               checkBoxSearchByProject.Checked,
               comboBoxProjectName.Text,
               checkBoxSearchByAuthor.Checked,
               comboBoxUser.SelectedItem != null ? (comboBoxUser.SelectedItem as User).Username : null,
               getStateAsText());
         }
      }

      private void linkLabelFindMe_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         selectCurrentUserInSearchDropdown();
      }

      private void textBoxSearchText_KeyDown(object sender, KeyEventArgs e)
      {
         onSearchTextBoxKeyDown(e.KeyCode);
      }

      private void textBoxSearchTargetBranch_KeyDown(object sender, KeyEventArgs e)
      {
         onSearchTextBoxKeyDown(e.KeyCode);
      }

      private void textBoxSearchText_TextChanged(object sender, EventArgs e)
      {
         onSearchTextBoxTextChanged(textBoxSearchTitleAndDescription, checkBoxSearchByTitleAndDescription);
      }

      private void textBoxSearchTargetBranch_TextChanged(object sender, EventArgs e)
      {
         onSearchTextBoxTextChanged(textBoxSearchTargetBranch, checkBoxSearchByTargetBranch);
      }

      private void comboBoxUser_SelectionChangeCommitted(object sender, EventArgs e)
      {
         onSearchComboBoxSelectionChangeCommitted(checkBoxSearchByAuthor);
      }

      private void comboBoxProjectName_SelectionChangeCommitted(object sender, EventArgs e)
      {
         onSearchComboBoxSelectionChangeCommitted(checkBoxSearchByProject);
      }

      private void checkBoxSearch_CheckedChanged(object sender, EventArgs e)
      {
         onSearchCheckBoxCheckedChanged();
      }

      private void comboBoxUser_Format(object sender, ListControlConvertEventArgs e)
      {
         formatUserListItem(e);
      }

      private string getStateAsText()
      {
         string stateToSearch = comboBoxSearchByState.SelectedItem.ToString();
         bool unspecifiedState = stateToSearch == "any";
         return unspecifiedState ? null : stateToSearch;
      }

      private void setTooltipsForSearchOptions()
      {
         toolTip.SetToolTip(this.checkBoxSearchByAuthor,
            "Search merge requests by author");
         toolTip.SetToolTip(this.checkBoxSearchByProject,
            "Search merge requests by project name");
         toolTip.SetToolTip(this.checkBoxSearchByTargetBranch,
            "Search merge requests by target branch name");
         toolTip.SetToolTip(this.checkBoxSearchByTitleAndDescription,
            "Search merge requests by words from title and description");

         void extendControlTooltip(Control control, int searchLimit) =>
            toolTip.SetToolTip(control, String.Format(
               "{0} (up to {1} results)", toolTip.GetToolTip(control), searchLimit));

         extendControlTooltip(checkBoxSearchByAuthor,
            Constants.MaxSearchResults);
         extendControlTooltip(checkBoxSearchByProject,
            Constants.MaxSearchResults);
         extendControlTooltip(checkBoxSearchByTargetBranch,
            Constants.MaxSearchResults);
         extendControlTooltip(checkBoxSearchByTitleAndDescription,
            Constants.MaxSearchResults);
      }

      private void fillProjectList(IEnumerable<string> projectNames)
      {
         if (!projectNames.Any())
         {
            comboBoxProjectName.Enabled = false;
            comboBoxProjectName.DropDownStyle = ComboBoxStyle.DropDown; // allows custom text
            comboBoxProjectName.Text = "Project list is empty or not loaded yet";
            checkBoxSearchByProject.Enabled = false;
         }
         else
         {
            WinFormsHelpers.FillComboBox(comboBoxProjectName,
               projectNames.OrderBy(projectName => projectName).ToArray(), _ => true);
         }
      }

      private void fillUserList(IEnumerable<User> users)
      {
         if (!users.Any())
         {
            comboBoxUser.Enabled = false;
            comboBoxUser.DropDownStyle = ComboBoxStyle.DropDown; // allows custom text
            comboBoxUser.Text = "User list is not loaded yet";
            checkBoxSearchByAuthor.Enabled = false;
         }
         else
         {
            WinFormsHelpers.FillComboBox(comboBoxUser,
               users.OrderBy(user => user.Name).ToArray(), _ => true);
         }
      }

      private void applyInitialState(EditSearchQueryFormState state)
      {
         if (state == null)
         {
            return;
         }

         textBoxSearchTargetBranch.Text = state.TargetBranchNameText;
         checkBoxSearchByTargetBranch.Checked = state.IsTargetBranchChecked;

         textBoxSearchTitleAndDescription.Text = state.TitleAndDescriptionText;
         checkBoxSearchByTitleAndDescription.Checked = state.IsTitleAndDescriptionChecked;

         if (!String.IsNullOrWhiteSpace(state.ProjectName))
         {
            string projectItem = comboBoxProjectName.Items
               .Cast<string>()
               .FirstOrDefault(item => item == state.ProjectName);
            if (projectItem != null)
            {
               comboBoxProjectName.SelectedItem = projectItem;
               checkBoxSearchByProject.Checked = state.IsProjectChecked;
            }
            else
            {
               checkBoxSearchByProject.Checked = false;
            }
         }
         else
         {
            checkBoxSearchByProject.Checked = state.IsProjectChecked;
         }

         string defaultUserName = String.IsNullOrEmpty(state.AuthorUserName)
            ? _currentUser.Username : state.AuthorUserName;
         User authorItem = comboBoxUser.Items
            .Cast<User>()
            .FirstOrDefault(item => item.Username == defaultUserName);
         if (authorItem != null)
         {
            comboBoxUser.SelectedItem = authorItem;
            checkBoxSearchByAuthor.Checked = state.IsAuthorNameChecked;
         }
         else
         {
            checkBoxSearchByAuthor.Checked = false;
         }

         string defaultState = !String.IsNullOrWhiteSpace(state.State)
            ? state.State : (string)comboBoxSearchByState.Items[0];
         string stateItem = comboBoxSearchByState.Items
            .Cast<string>()
            .FirstOrDefault(item => item == defaultState);
         if (stateItem != null)
         {
            comboBoxSearchByState.SelectedItem = stateItem;
         }
      }

      private void selectCurrentUserInSearchDropdown()
      {
         foreach (object item in comboBoxUser.Items)
         {
            if ((item as User).Name == getCurrentUser().Name)
            {
               comboBoxUser.SelectedItem = item;
               checkBoxSearchByAuthor.Checked = true;
               break;
            }
         }
      }

      private void updateOKButtonState()
      {
         buttonOK.Enabled =
              (checkBoxSearchByAuthor.Enabled
            && checkBoxSearchByAuthor.Checked
            && comboBoxUser.Enabled
            && comboBoxUser.SelectedItem != null)
         ||   (checkBoxSearchByProject.Enabled
            && checkBoxSearchByProject.Checked
            && comboBoxProjectName.Enabled
            && comboBoxProjectName.SelectedItem != null)
         ||   (checkBoxSearchByTargetBranch.Enabled
            && checkBoxSearchByTargetBranch.Checked
            && textBoxSearchTargetBranch.Enabled
            && !String.IsNullOrWhiteSpace(textBoxSearchTargetBranch.Text))
         ||   (checkBoxSearchByTitleAndDescription.Enabled
            && checkBoxSearchByTitleAndDescription.Checked
            && textBoxSearchTitleAndDescription.Enabled
            && !String.IsNullOrWhiteSpace(textBoxSearchTitleAndDescription.Text));
      }

      private void onSearchTextBoxKeyDown(Keys keys)
      {
         if (keys == Keys.Enter && buttonOK.Enabled)
         {
            buttonOK.PerformClick();
         }
      }

      private void onSearchTextBoxTextChanged(TextBox textBox, CheckBox associatedCheckBox)
      {
         associatedCheckBox.Checked = textBox.TextLength > 0;
         updateOKButtonState();
      }

      private void onSearchComboBoxSelectionChangeCommitted(CheckBox associatedCheckBox)
      {
         associatedCheckBox.Checked = true;
         updateOKButtonState();
      }

      private void onSearchCheckBoxCheckedChanged()
      {
         updateOKButtonState();
      }

      private static void formatUserListItem(ListControlConvertEventArgs e)
      {
         e.Value = (e.ListItem as User).Name;
      }

      private User getCurrentUser()
      {
         return _currentUser;
      }

      private readonly User _currentUser;
   }
}

