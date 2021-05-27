using System.Windows.Forms;

namespace mrHelper.App.Controls
{
   internal class ConnectionTabPage : TabPage
   {
      public ConnectionTabPage(string hostname, ConnectionPage connectionPage)
      {
         HostName = hostname;
         ConnectionPage = connectionPage;
         connectionPage.Dock = DockStyle.Fill;
         Controls.Add(connectionPage);
      }

      internal string HostName { get; }
      internal ConnectionPage ConnectionPage { get; }
   }
}

