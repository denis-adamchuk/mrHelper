using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace mrHelper.App.Helpers
{
   internal class TrayIcon
   {
      /// <summary>
      /// Tooltip timeout in seconds
      /// </summary>
      private const int notifyTooltipTimeout = 5;

      internal TrayIcon(NotifyIcon notifyIcon)
      {
         _notifyIcon = notifyIcon;
      }

      internal struct BalloonText
      {
         public BalloonText(string title, string text)
         {
            Title = title;
            Text = text;
         }

         public string Title { get; }
         public string Text { get; }
      }

      internal void ShowTooltipBalloon(BalloonText balloonText)
      {
         _notifyIcon.BalloonTipTitle = balloonText.Title;
         _notifyIcon.BalloonTipText = balloonText.Text;
         _notifyIcon.ShowBalloonTip(notifyTooltipTimeout);

         Trace.TraceInformation(String.Format("Tooltip: Title \"{0}\" Text \"{1}\"",
            balloonText.Title, balloonText.Text));
      }

      private readonly NotifyIcon _notifyIcon;
   }
}

