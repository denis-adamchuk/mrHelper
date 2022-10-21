using System;
using System.IO;

namespace mrHelper.Common.Tools
{
   public static class PathFinder
   {
      public static string NewEMailDirectory => Path.Combine(TempDirectory, EMailDirectoryPrefix, Guid.NewGuid().ToString());

      public static string DefaultStorage => Path.Combine(TempDirectory, StoragePrefix);

      public static string SnapshotStorage => Path.Combine(TempDirectory, SnapshotStoragePrefix);

      public static string InstallerStorage => Path.Combine(TempDirectory, InstallerStoragePrefix);

      public static string LogArchiveStorage => Path.Combine(TempDirectory, LogArchivePrefix);

      public static string DumpStorage => Path.Combine(TempDirectory, DumpStoragePrefix);

      public static string AvatarStorage => Path.Combine(TempDirectory, AvatarStorageDirName);

      private static string EMailDirectoryPrefix => "mrh_email";

      private static string StoragePrefix => "mrh_db";

      private static string SnapshotStoragePrefix => "mrh_snapshot";

      private static string LogArchivePrefix => "mrh_logs";

      private static string InstallerStoragePrefix => "mrh_msi";

      private static string DumpStoragePrefix => "mrh_dumps";

      private static string AvatarStorageDirName => "mrh_av";

      private static string TempDirectory => Environment.GetEnvironmentVariable("TEMP");
   }
}

