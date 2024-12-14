using System.Windows.Forms;

namespace mrHelper.App.Controls
{
   internal class ColorGroupConfigTabPage : TabPage
   {
      internal ColorGroupConfigTabPage(string name, ColorGroupConfigPage page)
      {
         page.Dock = DockStyle.Fill;
         Controls.Add(page);
         Name = "tabPage" + name;
         Padding = new System.Windows.Forms.Padding(3);
         Text = name;
         UseVisualStyleBackColor = true;
      }

      internal ColorGroupConfigPage Page => Controls.Count > 0 ? (ColorGroupConfigPage)Controls[0] : null;
   }

}
