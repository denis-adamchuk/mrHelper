using mrHelper.App.Forms;

namespace mrHelper.App.src.Forms
{
   public partial class CreateNewMergeRequestForm : CustomFontForm
   {
      public CreateNewMergeRequestForm()
      {
         CommonControls.Tools.WinFormsHelpers.FixNonStandardDPIIssue(this,
            (float)Common.Constants.Constants.FontSizeChoices["Design"], 96);
         InitializeComponent();
         CommonControls.Tools.WinFormsHelpers.LogScaleDimensions(this);

         applyFont(Program.Settings.MainWindowFontSizeName);
      }
   }
}

