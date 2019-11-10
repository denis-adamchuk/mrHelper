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

         var installPath = findApplicationPath(RegistryView.Registry64, applicationNames);
         if (installPath == null)
         {
            installPath = findApplicationPath(RegistryView.Registry32, applicationNames);
         }

         return installPath;
      }

      static private string findApplicationPath(RegistryView view, string[] applicationNames)
      {
         var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
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

