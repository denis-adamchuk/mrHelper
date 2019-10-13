using System.Windows.Forms;

namespace mrHelper.CommonControls
{
   public class TextBoxNoWheel : TextBox
   {
      protected override void WndProc(ref Message m)
      {
         const int WM_MOUSEWHEEL = 0x020A;
         if (m.Msg == WM_MOUSEWHEEL)
         {
            m.HWnd = this.Parent.Handle;
         }
         base.WndProc(ref m);
      }
   }
}

