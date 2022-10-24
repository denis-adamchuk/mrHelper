using System.Windows.Forms;
using System.Windows.Input;

namespace mrHelper.CommonControls.Tools
{
   public static class WPFHelpers
   {
      public static System.Windows.Controls.TextBox CreateWPFTextBox(
         System.Windows.Forms.Integration.ElementHost host, bool isReadOnly, string text, bool multiline,
         bool isSpellCheckEnabled, bool softwareOnlyRenderMode)
      {
         System.Windows.Controls.TextBox textbox = new System.Windows.Controls.TextBox
         {
            AcceptsReturn = multiline,
            IsReadOnly = isReadOnly,
            Text = text,
            SelectionStart = text.Length,
            TextWrapping = System.Windows.TextWrapping.Wrap,
            HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto
         };
         if (softwareOnlyRenderMode)
         {
            textbox.Loaded += (s, e) =>
            {
               var source = System.Windows.PresentationSource.FromVisual(textbox);
               if (source.CompositionTarget is System.Windows.Interop.HwndTarget hwndTarget)
               {
                  hwndTarget.RenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
               }
            };
         }
         textbox.SpellCheck.IsEnabled = isSpellCheckEnabled;
         host.Child = textbox;
         return textbox;
      }

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

