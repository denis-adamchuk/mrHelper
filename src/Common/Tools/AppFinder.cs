using Microsoft.Win32;
using System.Diagnostics;

namespace mrHelper.Common.Tools
{
   public static class AppFinder
   {
      public class AppInfo
      {
         public AppInfo(string installPath, string productCode)
         {
            InstallPath = installPath;
            ProductCode = productCode;
         }

         public string InstallPath { get; }
         public string ProductCode { get; }
      }

      static public AppInfo GetApplicationInfo(string[] applicationNames)
      {
         Debug.Assert(applicationNames != null);
         foreach (RegistryHive hive in new RegistryHive[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
         {
            foreach (RegistryView view in new RegistryView[] { RegistryView.Registry32, RegistryView.Registry64 })
            {
               AppInfo appInfo = findApplication(hive, view, applicationNames);
               if (appInfo != null)
               {
                  return appInfo;
               }
            }
         }
         return null;
      }

      static private AppInfo findApplication(RegistryHive hive, RegistryView view, string[] applicationNames)
      {
         var hklm = RegistryKey.OpenBaseKey(hive, view);
         var uninstall = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
         foreach (var productSubKey in uninstall.GetSubKeyNames())
         {
            var product = uninstall.OpenSubKey(productSubKey);
            var displayName = product.GetValue("DisplayName");
            foreach (var appName in applicationNames)
            {
               if (displayName != null && displayName.ToString().Contains(appName))
               {
                  return new AppInfo(product.GetValue("InstallLocation").ToString(), productSubKey);
               }
            }
         }
         return null;
      }
   }
}

