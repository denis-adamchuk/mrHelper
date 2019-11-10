using System;
using Microsoft.Win32;
using System.Diagnostics;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;

namespace mrHelper.DiffTool
{
   /// <summary>
   /// Performs integration of the application into the specific DiffTool. Registers special difftool in git.
   /// </summary>
   public class DiffToolIntegration
   {
      public const string GitDiffToolName = "mrhelperdiff";

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
         string toolpath = CommonTools.AppFinder.GetInstallPath(diffTool.GetToolRegistryNames());
         if (!isInstalled(toolpath))
         {
            throw new DiffToolNotInstalledException("Diff tool not installed", null);
         }

         Trace.TraceInformation(String.Format("Diff Tool installed at: {0}", toolpath));

         registerInGit(diffTool, GitDiffToolName, toolpath);

         try
         {
            registerInTool(diffTool);
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
      private void registerInGit(IIntegratedDiffTool diffTool, string name, string toolpath)
      {
         _globalGitConfiguration.SetGlobalDiffTool(name, getGitCommand(diffTool, toolpath));
      }

      public bool isInstalled(string toolpath)
      {
         return !String.IsNullOrEmpty(toolpath);
      }

      private string getGitCommand(IIntegratedDiffTool diffTool, string toolPath)
      {
         var path = System.IO.Path.Combine(toolPath, diffTool.GetToolCommand());
         path = path.Replace(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
         return "\"\\\"" + path + "\\\"" + diffTool.GetToolCommandArguments() + "\"";
      }

      private readonly IGlobalGitConfiguration _globalGitConfiguration;
   }
}

