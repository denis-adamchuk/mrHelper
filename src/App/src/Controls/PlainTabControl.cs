using System.Drawing;
using System.Windows.Forms;

namespace mrHelper.App.Controls
{
   internal class PlainTabControl : TabControl
   {
      public PlainTabControl()
      {
         Appearance = TabAppearance.FlatButtons;
         ItemSize = new Size(0, 1);
         SizeMode = TabSizeMode.Fixed;
      }

      protected override void OnKeyDown(KeyEventArgs e)
      {
         bool suppressKeyDown =
             (e.Control
                 && (e.KeyCode == Keys.Tab
                     || e.KeyCode == Keys.Next
                     || e.KeyCode == Keys.Prior));

         if (!suppressKeyDown)
         {
            base.OnKeyDown(e);
         }
      }
   }
}

