using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace mrHelper.Common.Tools
{
   public static class AppFinder
   {
      public class AppInfo
      {
         public AppInfo(string installPath, string productCode, string displayVersion)
         {
            InstallPath = installPath;
            ProductCode = productCode;
            DisplayVersion = displayVersion;
         }

         public string InstallPath { get; }
         public string ProductCode { get; }
         public string DisplayVersion { get; }
      }

      public enum MatchKind
      {
         Exact,
         Contains,
         StartsWith
      }

      static public AppInfo GetApplicationInfo(string[] applicationNames, MatchKind matchKind = MatchKind.Contains)
      {
         Debug.Assert(applicationNames != null);
         foreach (RegistryHive hive in new RegistryHive[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
         {
            foreach (RegistryView view in new RegistryView[] { RegistryView.Registry32, RegistryView.Registry64 })
            {
               AppInfo appInfo = findApplication(hive, view, applicationNames, matchKind);
               if (appInfo != null)
               {
                  return appInfo;
               }
            }
         }
         return null;
      }

      static private AppInfo findApplication(RegistryHive hive, RegistryView view, string[] applicationNames,
         MatchKind matchKind)
      {
         try
         {
            return findApplicationSafe(hive, view, applicationNames, matchKind);
         }
         catch (Exception ex)
         {
            Trace.TraceError(
               "[AppFinder] An exception occurred on attempt to access the registry: {0}",
               ex.ToString());
         }
         return null;
      }

      static private AppInfo findApplicationSafe(RegistryHive hive, RegistryView view, string[] applicationNames,
         MatchKind matchKind)
      {
         RegistryKey hklm = RegistryKey.OpenBaseKey(hive, view);
         RegistryKey uninstall = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
         foreach (string productSubKey in uninstall.GetSubKeyNames())
         {
            RegistryKey product = uninstall.OpenSubKey(productSubKey);
            object displayName = product.GetValue("DisplayName");
            if (displayName == null)
            {
               continue;
            }
            foreach (string appName in applicationNames)
            {
               bool match = false;
               StringComparison comparison = StringComparison.InvariantCultureIgnoreCase;
               switch (matchKind)
               {
                  case MatchKind.Exact:
                     match = displayName.ToString().Equals(appName, comparison);
                     break;
                  case MatchKind.Contains:
                     match = displayName.ToString().Contains(appName);
                     break;
                  case MatchKind.StartsWith:
                     match = displayName.ToString().StartsWith(appName, comparison);
                     break;
               }
               if (match)
               {
                  object installLocation = product.GetValue("InstallLocation");
                  string installLocationString = installLocation?.ToString() ?? String.Empty;
                  object displayVersion = product.GetValue("DisplayVersion");
                  string displayVersionString = displayVersion?.ToString() ?? String.Empty;
                  return new AppInfo(installLocationString, productSubKey, displayVersionString);
               }
            }
         }
         return null;
      }
   }
}

