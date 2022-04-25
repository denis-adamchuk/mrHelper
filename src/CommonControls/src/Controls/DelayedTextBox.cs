using System;
using System.Windows.Forms;

namespace mrHelper.CommonControls.Controls
{
   public partial class DelayedTextBox : TextBox
   {
      public DelayedTextBox()
      {
         _delayedInputTimer = new System.Timers.Timer
         {
            Interval = DelayPeriod,
            AutoReset = false,
            SynchronizingObject = this
         };
         _delayedInputTimer.Elapsed += (_, e) => base.OnTextChanged(e);
      }

      public void SetTextImmediately(string text)
      {
         _applyImmediately = true;
         Text = text;
         _applyImmediately = false;
      }

      /// <summary> 
      /// Clean up any resources being used.
      /// </summary>
      /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
      protected override void Dispose(bool disposing)
      {
         _delayedInputTimer?.Dispose();
         base.Dispose(disposing);
      }

      protected override void OnTextChanged(EventArgs e)
      {
         if (_applyImmediately)
         {
            base.OnTextChanged(e);
            return;
         }

         if (_delayedInputTimer.Enabled)
         {
            _delayedInputTimer.Stop();
         }
         _delayedInputTimer.Start();
      }

      private readonly System.Timers.Timer _delayedInputTimer;
      private bool _applyImmediately;

      private static readonly int DelayPeriod = 250; // ms
   }
}

