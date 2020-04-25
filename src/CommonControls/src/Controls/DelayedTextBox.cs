using System;
using System.Windows.Forms;

namespace mrHelper.CommonControls.Controls
{
   public partial class DelayedTextBox : TextBox
   {
      public DelayedTextBox()
      {
         InitializeComponent();

         _delayedInputTimer = new System.Timers.Timer
         {
            Interval = DelayPeriod,
            AutoReset = false,
            SynchronizingObject = this
         };
         _delayedInputTimer.Elapsed += (_, e) => base.OnTextChanged(e);
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

