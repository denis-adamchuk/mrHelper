using System;
using Microsoft.Win32;
using System.Diagnostics;
using mrHelper.Common.Interfaces;

namespace mrHelper.DiffTool
{
   public const string GitDiffToolName = "mrhelperdiff";

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
      public DiffToolIntegration(IGlobalGitConfiguration globalGitConfiguration)
      {
         _globalGitConfiguration = globalGitConfiguration;
      }

      /// <summary>
      /// Throws GitOperationException if integration failed
      /// Throws DiffToolIntegrationException if diff tool is not installed
      /// </summary>
      public void Integrate(IIntegratedDiffTool diffTool)
      {
         registerInGit(GitDiffToolName);

         try
         {
            registerInTool();
         }
         catch (DiffToolIntegrationException)
         {
            Trace.TraceError(String.Format("Cannot register the application in \"{0}\"", GitDiffToolName));

            try
            {
               _globalGitConfiguration.RemoveGlobalDiffTool(GitDiffToolName);
            }
            catch (GitOperationException)
            {
               Trace.TraceError(String.Format("Cannot remove \"{0}\" from git config", GitDiffToolName));
            }

            throw;
         }
      }

      /// <summary>
      /// Throws DiffToolIntegrationException if integration failed
      /// </summary>
      private void registerInTool(IIntegratedDiffTool diffTool)
      {
         if (!isInstalled())
         {
            throw new DiffToolIntegrationException("Diff tool not installed", null);
         }

         try
         {
            diffTool.PatchToolConfig(Process.GetCurrentProcess().MainModule.FileName + " diff");
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
      /// Throws DiffToolIntegrationException if diff tool is not installed
      /// </summary>
      private void registerInGit(IIntegratedDiffTool diffTool, string name)
      {
         if (!isInstalled())
         {
            throw new DiffToolIntegrationException("Diff tool not installed", null);
         }

         _globalGitConfiguration.SetGlobalDiffTool(name, getGitCommand());
      }

      public bool isInstalled(IIntegratedDiffTool diffTool)
      {
         return getToolPath(diffTool) != null;
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

      private string getGitCommand(IIntegratedDiffTool diffTool)
      {
         string toolPath = getToolPath(diffTool);
         if (toolPath == null)
         {
            throw new DiffToolIntegrationException(String.Format("Cannot find installation location in registry"));
         }

         var path = System.IO.Path.Combine(toolPath, diffTool.GetToolCommand());
         path = path.Replace(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
         return "\"\\\"" + path + "\\\"" + diffTool.GetToolCommandArguments() + "\"";
      }

      private string getToolPath(IIntegratedDiffTool diffTool)
      {
         return getInstallPath(diffTool.GetToolRegistryNames());
      }

      private IGlobalGitConfiguration _globalGitConfiguration;
   }
}

