using System;
using Microsoft.Win32;
using mrCore;
using System.Diagnostics;

namespace mrDiffTool
{
   public class DiffToolIntegrationException : Exception
   {
      public DiffToolIntegrationException(string message, Exception ex = null)
         : base(String.Format(message))
      {
         NestedException = ex;
      }

      public Exception NestedException { get; }
   }

   /// <summary>
   /// Performs integration of the application into the specific DiffTool. Registers special difftool in git.
   /// </summary>
   public class DiffToolIntegration
   {
      public DiffToolIntegration(IntegratedDiffTool diffTool)
      {
         _diffTool = diffTool;
      }

      /// <summary>
      /// Throws DiffToolIntegrationException if integration failed
      /// </summary>
      public void RegisterInTool()
      {
         if (!isInstalled())
         {
            throw new DiffToolIntegrationException("Diff tool not installed", null);
         }

         try
         {
            _diffTool.PatchToolConfig(Process.GetCurrentProcess().MainModule.FileName + " diff");
         }
         catch (DiffToolIntegrationException)
         {
            throw;
         }
         catch (Exception ex) // whatever XML exception
         {
            throw new DiffToolIntegrationException("Unknown error", ex);
         }
      }

      /// <summary>
      /// Throws GitOperationException if integration failed
      /// Throws DiffToolIntegrationException if diff tooll is not installed
      /// </summary>
      public void RegisterInGit(string name)
      {
         if (!isInstalled())
         {
            throw new DiffToolIntegrationException("Diff tool not installed", null);
         }

         GitUtils.SetGlobalDiffTool(name, getGitCommand());
      }

      public bool isInstalled()
      {
         return getToolPath() != null;
      }

      static private string getInstallPath(string[] applicationNames)
      {
         Debug.Assert(applicationNames != null);

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
         string toolPath = getToolPath();
         if (toolPath == null)
         {
            throw new DiffToolIntegrationException(String.Format("Cannot find installation location in registry"));
         }

         var path = System.IO.Path.Combine(toolPath, _diffTool.GetToolCommand());
         path = path.Replace(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
         return "\"\\\"" + path + "\\\"" + _diffTool.GetToolCommandArguments() + "\"";
      }

      private string getToolPath()
      {
         return getInstallPath(_diffTool.GetToolRegistryNames());
      }

      private readonly IntegratedDiffTool _diffTool;
   }
}

