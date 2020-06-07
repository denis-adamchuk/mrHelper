using Microsoft.Win32;
using System.Collections.Generic;
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

         public override bool Equals(object obj)
         {
            return obj is AppInfo info &&
                   InstallPath == info.InstallPath &&
                   ProductCode == info.ProductCode;
         }

         public override int GetHashCode()
         {
            int hashCode = -602943768;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(InstallPath);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ProductCode);
            return hashCode;
         }
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
         RegistryKey hklm = RegistryKey.OpenBaseKey(hive, view);
         RegistryKey uninstall = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
         foreach (string productSubKey in uninstall.GetSubKeyNames())
         {
            RegistryKey product = uninstall.OpenSubKey(productSubKey);
            object displayName = product.GetValue("DisplayName");
            foreach (string appName in applicationNames)
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

