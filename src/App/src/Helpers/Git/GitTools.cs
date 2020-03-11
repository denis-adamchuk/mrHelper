using System;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;
using mrHelper.GitClient;

namespace mrHelper.App.Helpers
{
   public static class GitTools
   {
      public class SSLVerificationDisableException : ExceptionEx
      {
         internal SSLVerificationDisableException(Exception innerException)
            : base(String.Empty, innerException)
         {
         }
      }

      public static void DisableSSLVerification()
      {
         try
         {
            ExternalProcess.Start("git", "config --global http.sslVerify false", true, String.Empty);
         }
         catch (Exception ex)
         {
            if (ex is ExternalProcessFailureException || ex is ExternalProcessSystemException)
            {
               throw new SSLVerificationDisableException(ex);
            }
            throw;
         }
      }
   }
}
