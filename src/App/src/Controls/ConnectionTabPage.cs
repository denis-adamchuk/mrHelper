using System.Windows.Forms;

namespace mrHelper.App.Controls
{
   internal class ConnectionTabPage : TabPage
   {
      public ConnectionTabPage(string hostname, ConnectionPage connectionPage)
      {
         HostName = hostname;
         connectionPage.Dock = DockStyle.Fill;
         Controls.Add(connectionPage);
      }

      internal string HostName { get; }
      internal ConnectionPage ConnectionPage => Controls.Count > 0 ? (ConnectionPage)Controls[0] : null;
   }
}

