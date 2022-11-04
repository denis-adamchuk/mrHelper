using System.Windows.Forms;
using mrHelper.CommonControls.Tools;

namespace mrHelper.App.Controls
{
   internal class AvatarBox : PictureBox
   {
      protected override void OnPaint(PaintEventArgs e) =>
         WinFormsHelpers.DrawClippedCircleImage(e.Graphics, Image, ClientRectangle);
   }
}

