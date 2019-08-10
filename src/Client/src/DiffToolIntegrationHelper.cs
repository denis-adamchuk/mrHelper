using System;
using mrHelper.DiffTool;

namespace mrHelper.Client
{
   public static class DiffToolIntegrationHelper
   {
      public static void IntegrateDiffTool(string gitDiffToolName)
      {
         IntegratedDiffTool diffTool = new BC3Tool();
         DiffToolIntegration integration = new DiffToolIntegration(diffTool);

         try
         {
            integration.RegisterInGit(gitDiffToolName);
         }
         catch (Exception ex)
         {
            if (ex is DiffToolIntegrationException || ex is GitOperationException)
            {
               ExceptionHandlers.Handle(ex,
                  String.Format("Cannot integrate \"{0}\" in git", diffTool.GetToolName()), true);
               return;
            }
            throw;
         }

         try
         {
            integration.RegisterInTool();
         }
         catch (DiffToolIntegrationException ex)
         {
            ExceptionHandlers.Handle(ex,
               String.Format("Cannot integrate the application in \"{0}\"", diffTool.GetToolName()), true);

            try
            {
               GitUtils.RemoveGlobalDiffTool(GitDiffToolName);
            }
            catch (GitOperationException ex2)
            {
               ExceptionHandlers.Handle(ex2,
                  String.Format("Cannot remove \"{0}\" from git config", GitDiffToolName), false);
            }
         }
      }
   }
}
