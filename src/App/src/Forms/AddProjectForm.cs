using System.Windows.Forms;

namespace mrHelper.App.Forms
{
   internal partial class AddProjectForm : Form
   {
      internal AddProjectForm()
      {
         InitializeComponent();
      }

      internal string ProjectName => textBoxProjectName.Text;

      private void textBox_KeyDown(object sender, KeyEventArgs e)
      {
         if (e.KeyCode == Keys.Enter)
         {
            e.Handled = false;

            buttonOK.PerformClick(); 
         }
      }
   }
}

