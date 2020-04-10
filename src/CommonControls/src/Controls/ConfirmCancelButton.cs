using System;
using System.Windows.Forms;

namespace mrHelper.CommonControls.Controls
{
   public partial class ConfirmCancelButton : Button
   {
      public ConfirmCancelButton()
      {
         InitializeComponent();
      }

      public string ConfirmationText { get; set; } = DefaultConfirmationText;

      public Func<bool> ConfirmationCondition { set { _confirmationCondition = value; } }

      protected override void OnClick(EventArgs e)
      {
         bool needToConfirm = _confirmationCondition?.Invoke() ?? true;
         if (!needToConfirm || Tools.WinFormsHelpers.ShowConfirmationDialog(ConfirmationText))
         {
            base.OnClick(e);
         }
      }

      private static string DefaultConfirmationText = "All changes will be lost, are you sure?";

      private Func<bool> _confirmationCondition;
   }
}

