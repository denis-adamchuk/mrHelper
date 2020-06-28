using System;
using System.Diagnostics;
using mrHelper.Common.Tools;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;

namespace mrHelper.DiffTool
{
   /// <summary>
   /// Performs integration of the application into the specific DiffTool. Registers special difftool in git.
   /// </summary>
   public class DiffToolIntegration
   {
      /// <summary>
      /// Throws DiffToolNotInstalledException if diff tool is not installed
      /// Throws DiffToolIntegrationException if integration failed
      /// </summary>
      public void Integrate(IIntegratedDiffTool diffTool, string self)
      {
         AppFinder.AppInfo appInfo = AppFinder.GetApplicationInfo(diffTool.GetToolRegistryNames());
         if (appInfo == null || !isInstalled(appInfo.InstallPath))
         {
            throw new DiffToolNotInstalledException("Diff tool not installed");
         }

         string toolpath = appInfo.InstallPath;
         Trace.TraceInformation(String.Format("Diff Tool installed at: {0}", toolpath));

         registerInGit(diffTool, toolpath);

         try
         {
            registerInTool(diffTool, self);
         }
         catch (DiffToolIntegrationException)
         {
            Trace.TraceError(String.Format("Cannot register the application in \"{0}\"", Constants.GitDiffToolName));

            try
            {
               string key = String.Format("difftool.{0}.cmd", Constants.GitDiffToolName);
               GitTools.SetConfigKeyValue(GitTools.ConfigScope.Global, key, null, String.Empty);
            }
            catch (Exception ex)
            {
               if (ex is ExternalProcessFailureException || ex is ExternalProcessSystemException)
               {
                  Trace.TraceError(String.Format("Cannot remove \"{0}\" from git config", Constants.GitDiffToolName));
               }
            }

            throw;
         }
      }

      /// <summary>
      /// Throws DiffToolIntegrationException if integration failed
      /// </summary>
      private void registerInTool(IIntegratedDiffTool diffTool, string self)
      {
         try
         {
            diffTool.PatchToolConfig(self + " diff");
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
      /// Throws ExternalProcessFailureException/ExternalProcessSystemException if registration failed
      /// </summary>
      private void registerInGit(IIntegratedDiffTool diffTool, string toolpath)
      {
         string value = getGitCommand(diffTool, toolpath);
         GitTools.SetConfigKeyValue(GitTools.ConfigScope.Global, Constants.GitDiffToolConfigKey, value, String.Empty);
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
   }
}

