using Microsoft.Win32;
using System.Collections;
using System.ComponentModel;

// From http://www.mikebevers.be/blog/2010/01/setup-project-product-installlocation-in-registry-is-empty/
namespace mrHelper.InstallerCustomActions
{
   [RunInstaller(true)]
   public partial class InstallerClass : System.Configuration.Install.Installer
   {
      public InstallerClass()
      {
         InitializeComponent();
      }

      public override void Install(IDictionary stateSaver)
      {
         base.Install(stateSaver);

         stateSaver.Add("TargetDir", Context.Parameters["DP_TargetDir"].ToString());
         stateSaver.Add("ProductID", Context.Parameters["DP_ProductID"].ToString());
      }

      public override void Commit(IDictionary savedState)
      {
         base.Commit(savedState);

         string productId = savedState["ProductID"].ToString();
         RegistryKey applicationRegistry =
            Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\" + productId, true);

         if (applicationRegistry != null)
         {
            applicationRegistry.SetValue("InstallLocation", savedState["TargetDir"].ToString());
            applicationRegistry.Close();
         }
      }
   }
}

