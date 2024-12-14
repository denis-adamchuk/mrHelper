using System.ComponentModel;
using System.Windows.Forms;

namespace mrHelper.App.Controls
{

class ToolStripSystemTextBox : ToolStripControlHost
{
   internal ToolStripSystemTextBox()
      : base(new TextBox())
   {
   }

   [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
   [TypeConverter(typeof(ExpandableObjectConverter))]
   internal TextBox TextBox { get { return Control as TextBox; } }
}

}
