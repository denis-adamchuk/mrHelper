using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace mrHelper.App.Controls
{
   public class AvatarPanel : Panel
   {
      public AvatarPanel(Image avatarImage)
      {
         _avatarImage = avatarImage;
      }

      protected override void OnPaint(PaintEventArgs e)
      {
         if (_avatarImage != null)
         {
            e.Graphics.DrawImage(_avatarImage, e.ClipRectangle);
         }
      }

      private readonly Image _avatarImage;
   }
}

