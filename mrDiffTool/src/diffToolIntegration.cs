using System;
using Microsoft.Win32;
using mrCore;
using System.Diagnostics;

namespace mrDiffTool
{
   public class DiffToolIntegrationException : Exception
   {
      public DiffToolIntegrationException(string toolname, string reason)
         : base(String.Format("Cannot integrate mrHelper in \"{0}\". Reason: {1}", toolname, reason))
      {
      }
   }

   public class GitIntegrationException : Exception
   {
      public GitIntegrationException(string toolname, string command)
         : base(String.Format("Cannot set global git diff tool \"{0}\" with command \"{1}\".", toolname, command))
      {
      }
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
            return;
         }

         _diffTool.PatchToolConfig(Process.GetCurrentProcess().MainModule.FileName + " diff");
      }

      /// <summary>
      /// Throws GitIntegrationException if integration failed
      /// </summary>
      public void RegisterInGit(string name)
      {
         if (!isInstalled())
         {
            return;
         }

         try
         {
            GitUtils.SetGlobalDiffTool(name, getGitCommand());
         }
         catch (GitOperationException ex)
         {
            Trace.TraceError("GitOperationException: {0} Details:\n{1}", ex.Message, ex.Details);

            throw new GitIntegrationException(name, getGitCommand());
         }
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
            throw new DiffToolIntegrationException(_diffTool.GetToolName(),
               String.Format("Cannot find installation location in registry"));
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

