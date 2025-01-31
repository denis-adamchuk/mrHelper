using System.Drawing;
using System.Windows.Forms;

namespace mrHelper.CommonControls.Controls
{
   public class PlainTabControl : TabControl
   {
      public PlainTabControl()
      {
         // From "Hide Tab Header on C# TabControl" (https://stackoverflow.com/a/30231315)
         // Note that .Designer.cs files shall not override this with other values.
         // {
         Appearance = TabAppearance.FlatButtons;
         ItemSize = new Size(0, 1);
         SizeMode = TabSizeMode.Fixed;
         // }

         // Use OwnerDraw to paint a dark background in Dark theme.
         DrawMode = TabDrawMode.OwnerDrawFixed;
         SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer, true);
      }

      protected override void OnPaintBackground(PaintEventArgs args)
      {
         base.OnPaintBackground(args);
         using (SolidBrush backColor = new SolidBrush(Parent.BackColor))
         {
            args.Graphics.FillRectangle(backColor, ClientRectangle);
         }
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

