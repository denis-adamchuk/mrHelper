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
         if (_delayedInputTimer.Enabled)
         {
            _delayedInputTimer.Stop();
         }
         _delayedInputTimer.Start();
      }

      private System.Timers.Timer _delayedInputTimer;

      private static int DelayPeriod = 250; // ms
   }
}

