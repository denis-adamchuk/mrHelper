using System;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using mrHelper.App.Helpers;
using mrHelper.Common.Tools;

namespace mrHelper.App.Forms
{
   internal partial class AddKnownHostForm : ThemedForm
   {
      internal AddKnownHostForm()
      {
         InitializeComponent();

         applyFont(Program.Settings.MainWindowFontSizeName);

         linkLabelCreateAccessToken.Text = String.Empty;
         linkLabelCreateAccessToken.SetLinkLabelClicked(UrlHelper.OpenBrowser);
      }

      internal string Host => textBoxHost.Text;

      internal string AccessToken => textBoxAccessToken.Text;

      private void textBox_KeyDown(object sender, KeyEventArgs e)
      {
         if (e.KeyCode == Keys.Enter && Control.ModifierKeys == Keys.Control)
         {
            buttonOK.PerformClick();
         }
      }

      private static readonly string RegEx =
         @"^(http\:\/\/|https\:\/\/)?([a-z0-9][a-z0-9\-]*\.)+[a-z0-9][a-z0-9\-]*$";
      private static readonly Regex WebSiteRe = new Regex(RegEx, RegexOptions.Compiled | RegexOptions.IgnoreCase);

      private void textBoxHost_TextChanged(object sender, System.EventArgs e)
      {
         updateLinkLabel();
         Match m = WebSiteRe.Match(textBoxHost.Text);
         linkLabelCreateAccessToken.Enabled = m.Success;
      }

      private void updateLinkLabel()
      {
         string hostname = textBoxHost.Text;
         linkLabelCreateAccessToken.Text = GitLabClient.Helpers.GetCreateAccessTokenUrl(hostname);
      }
   }
}

