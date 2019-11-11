using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrHelper.CommonTools
{
   public static class AppFinder
   {
      static public string GetInstallPath(string[] applicationNames)
      {
         Debug.Assert(applicationNames != null);
         foreach (RegistryHive hive in new RegistryHive[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
         {
            foreach (RegistryView view in new RegistryView[] { RegistryView.Registry32, RegistryView.Registry64 })
            {
               string installPath = findApplicationPath(hive, view, applicationNames);
               if (!String.IsNullOrEmpty(installPath))
               {
                  return installPath;
               }
            }
         }
         return null;
      }

      static private string findApplicationPath(RegistryHive hive, RegistryView view, string[] applicationNames)
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
                  return product.GetValue("InstallLocation").ToString();
               }
            }
         }
         return null;
      }
   }
}

