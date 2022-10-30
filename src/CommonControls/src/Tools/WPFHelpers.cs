using System.Windows.Forms;
using System.Windows.Input;

namespace mrHelper.CommonControls.Tools
{
   public static class WPFHelpers
   {
      public static System.Windows.Forms.Keys GetKeysOnWPFKeyDown(System.Windows.Input.Key inputKey)
      {
         Keys keys = (Keys)System.Windows.Input.KeyInterop.VirtualKeyFromKey(inputKey);
         if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
         {
            keys |= Keys.Control;
         }
         if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
         {
            keys |= Keys.Shift;
         }
         return keys;
      }
   }
}

