using Microsoft.Win32;
using mrCore;
using System.Diagnostics;

namespace mrDiffTool
{
   public class DiffToolIntegration
   {
      public DiffToolIntegration(IntegratedDiffTool diffTool)
      {
         _diffTool = diffTool;
      }

      public bool IsInstalled()
      {
         return getToolPath() != null;
      }

      public void RegisterInTool()
      {
         if (!IsInstalled())
         {
            return;
         }

         _diffTool.PatchToolConfig(Process.GetCurrentProcess().MainModule.FileName + " diff");
      }

      public void RegisterInGit(string name)
      {
         if (!IsInstalled())
         {
            return;
         }

         GitRepository.SetGlobalDiffTool(name, getGitCommand());
      }

      static private string getInstallPath(string[] applicationNames)
      {
         if (applicationNames == null)
         {
            return null;
         }

         var installPath = findApplicationPath(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            applicationNames);
         if (installPath == null)
         {
            installPath = findApplicationPath(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall",
               applicationNames);
         }
         return installPath;
      }

      static private string findApplicationPath(string keyPath, string[] applicationNames)
      {
         var hklm = Registry.LocalMachine;
         var uninstall = hklm.OpenSubKey(keyPath);
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

      private string getGitCommand()
      {
         Debug.Assert(getToolPath() != null);
         var path = System.IO.Path.Combine(getToolPath(), _diffTool.GetToolCommand());
         path = path.Replace(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
         return "\"\\\"" + path + "\\\"" + _diffTool.GetToolCommandArguments() + "\"";
      }

      private string getToolPath()
      {
         return getInstallPath(_diffTool.GetToolNames());
      }

      private readonly IntegratedDiffTool _diffTool;
   }
}
