using System;
using System.Windows.Forms;
using System.Diagnostics;
using mrHelper.CommonControls.Tools;

namespace mrHelper.App.Forms
{
   internal partial class WrongActionConfirmationForm : Form
   {
      internal WrongActionConfirmationForm()
      {
         InitializeComponent();
      }

      internal void SetText(string text)
      {
         labelConfirmationText.Text = text;
      }

      protected override void OnLoad(EventArgs e)
      {
         base.OnLoad(e);
         ActiveControl = buttonNo;
      }

      internal enum ActionType
      {
         AddComment,
         CreateDiscussion,
         ExecuteAction
      }

      internal static bool Show(Control parent, ActionType type, Action goToCurrent)
      {
         WrongActionConfirmationForm actionConfirmationForm = new WrongActionConfirmationForm();
         actionConfirmationForm.SetText(getConfirmationText(type));
         WinFormsHelpers.PositionDialogOnControl(actionConfirmationForm, parent);
         switch (actionConfirmationForm.ShowDialog())
         {
            case System.Windows.Forms.DialogResult.Yes:
               return true;
            case System.Windows.Forms.DialogResult.No:
               return false;
            case System.Windows.Forms.DialogResult.Ignore:
               goToCurrent();
               return false;
         }
         return false;
      }

      private static string getConfirmationText(ActionType type)
      {
         switch (type)
         {
            case ActionType.AddComment:
               return AddCommentText;
            case ActionType.CreateDiscussion:
               return CreateDiscussionText;
            case ActionType.ExecuteAction:
               return ExecuteActionText;
         }
         Debug.Assert(false);
         return String.Empty;
      }

      private static readonly string AddCommentText =
         "You are going to add a comment to the wrong merge request that you are currently tracking time on. Are you sure?";
      private static readonly string CreateDiscussionText =
         "You are going to create a discussion to the wrong merge request that you are currently tracking time on. Are you sure?";
      private static readonly string ExecuteActionText =
         "You are going to run a command for the wrong merge request that you are currently tracking time on. Are you sure?";
   }
}
