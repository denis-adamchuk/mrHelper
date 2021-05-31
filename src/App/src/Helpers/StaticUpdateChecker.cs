using System;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading.Tasks;
using mrHelper.Common.Exceptions;
using static mrHelper.App.Helpers.ServiceManager;
using mrHelper.Common.Tools;

namespace mrHelper.App.Helpers
{
   internal class VersionInformation
   {
      public VersionInformation(string versionNumber, string installerFilePath)
      {
         VersionNumber = versionNumber;
         InstallerFilePath = installerFilePath;
      }

      internal string VersionNumber;
      internal string InstallerFilePath;
   }

   internal static class StaticUpdateChecker
   {
      internal static Task CheckForUpdatesAsync(ServiceManager serviceManager)
      {
         return checkForApplicationUpdatesAsync(serviceManager);
      }

      internal static VersionInformation NewVersionInformation
      {
         get
         {
            lock (mylock)
            {
               return _newVersionInformation;
            }
         }
         set
         {
            lock (mylock)
            {
               _newVersionInformation = value;
            }
         }
      }

      async private static Task checkForApplicationUpdatesAsync(ServiceManager serviceManager)
      {
         Trace.TraceInformation("[StaticUpdateChecker] Checking ServiceManager for the latest version information...");
         LatestVersionInformation info = getLatestVersionInformationFromServer(serviceManager);
         if (info == null)
         {
            return;
         }

         Trace.TraceInformation(String.Format("[StaticUpdateChecker] New version {0} is found", info.VersionNumber));
         if (String.IsNullOrEmpty(info.InstallerFilePath) || !System.IO.File.Exists(info.InstallerFilePath))
         {
            Trace.TraceWarning(String.Format("[StaticUpdateChecker] Installer cannot be found at \"{0}\"",
               info.InstallerFilePath));
            return;
         }

         await Task.Run(() => copyNewVersionFromServer(info));
      }

      private static LatestVersionInformation getLatestVersionInformationFromServer(ServiceManager serviceManager)
      {
         LatestVersionInformation info = serviceManager.GetLatestVersionInfo();
         if (info == null || String.IsNullOrWhiteSpace(info.VersionNumber))
         {
            return null;
         }

         try
         {
            System.Version currentVersion = new System.Version(Application.ProductVersion);
            System.Version latestVersion = new System.Version(info.VersionNumber);
            if (currentVersion >= latestVersion)
            {
               return null;
            }

            if (NewVersionInformation != null)
            {
               System.Version cachedLatestVersion = new System.Version(NewVersionInformation.VersionNumber);
               if (cachedLatestVersion >= latestVersion)
               {
                  return null;
               }
            }
         }
         catch (ArgumentException ex)
         {
            ExceptionHandlers.Handle("Wrong version number", ex);
            return null;
         }

         return info;
      }

      private static void copyNewVersionFromServer(LatestVersionInformation info)
      {
         if (info == null)
         {
            return;
         }

         if (!Directory.Exists(PathFinder.InstallerStorage))
         {
            try
            {
               Directory.CreateDirectory(PathFinder.InstallerStorage);
            }
            catch (Exception ex) // Any exception from Directory.CreateDirectory()
            {
               ExceptionHandlers.Handle("Cannot create a directory for installer", ex);
               return;
            }
         }

         string filename = Path.GetFileName(info.InstallerFilePath);
         string destFilePath = Path.Combine(PathFinder.InstallerStorage, filename);

         Debug.Assert(!System.IO.File.Exists(destFilePath));

         Trace.TraceInformation(String.Format(
            "[StaticUpdateChecker] Copying from \"{0}\" to \"{1}\"...", info.InstallerFilePath, destFilePath));
         try
         {
            System.IO.File.Copy(info.InstallerFilePath, destFilePath);
         }
         catch (Exception ex) // Any exception from System.IO.File.Copy()
         {
            ExceptionHandlers.Handle("Cannot download a new version", ex);
            return;
         }

         Trace.TraceInformation("[StaticUpdateChecker] File copied");
         onNewVersionCopiedFromServer(destFilePath, info.VersionNumber);
      }

      private static void onNewVersionCopiedFromServer(string filePath, string versionNumber)
      {
         NewVersionInformation = new VersionInformation(versionNumber, filePath);
      }

      private static readonly object mylock = new object();
      private static VersionInformation _newVersionInformation;
   }
}

