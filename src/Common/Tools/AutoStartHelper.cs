using System.Diagnostics;
using Microsoft.Win32;

namespace mrHelper.Common.Tools
{
   static public class AutoStartHelper
   {
      static public void ApplyAutostartSetting(bool enabled, string appName, string command)
      {
         try
         {
            string currentCommand = getAutostartCommand(appName);
            if (enabled)
            {
               if (currentCommand == null || currentCommand != command)
               {
                  setAutostartCommand(appName, command);
               }
            }
            else if (currentCommand != null)
            {
               delAutostartCommand(appName);
            }
         }
         catch (System.Exception ex)
         {
            Trace.TraceError(
               "[AutoStartHelper] An exception occurred on attempt to access the registry: {0}",
               ex.ToString());
         }
      }

      static private string getAutostartCommand(string appName)
      {
         RegistryKey run = getRunKey(false);
         object value = run.GetValue(appName, null);
         return value != null && run.GetValueKind(appName) == RegistryValueKind.String ? (string)value : null;
      }

      static private void setAutostartCommand(string appName, string command)
      {
         getRunKey(true).SetValue(appName, command);
      }

      static private void delAutostartCommand(string appName)
      {
         getRunKey(true).DeleteValue(appName, false);
      }

      static private RegistryKey getRunKey(bool writable)
      {
         RegistryHive hive = RegistryHive.CurrentUser;
         RegistryView view = RegistryView.Registry32;
         RegistryKey hklm = RegistryKey.OpenBaseKey(hive, view);
         return hklm.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", writable);
      }
   }
}

