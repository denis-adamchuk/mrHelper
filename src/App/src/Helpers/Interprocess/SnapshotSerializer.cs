using mrHelper.Common.Exceptions;
using mrHelper.Common.Tools;
using System;

namespace mrHelper.App.Interprocess
{
   /// <summary>
   /// Encapsulates work with the interprocess snapshot.
   /// </summary>
   public class SnapshotSerializer
   {
      private static readonly string snapshotPath = PathFinder.SnapshotStorage;

      /// <summary>
      /// Serializes snapshot to disk.
      /// </summary>
      public void SerializeToDisk(Snapshot snapshot, int pid)
      {
         string filename = String.Format("mrHelper.snapshot.{0}.json", pid);
         if (!System.IO.Directory.Exists(snapshotPath))
         {
            try
            {
               System.IO.Directory.CreateDirectory(snapshotPath);
            }
            catch (Exception ex) // Any exception from Directory.CreateDirectory()
            {
               ExceptionHandlers.Handle("Cannot create a directory for snapshot", ex);
               return;
            }
         }
         JsonUtils.SaveToFile(System.IO.Path.Combine(snapshotPath, filename), snapshot);
      }

      /// <summary>
      /// Loads snapshot from disk and de-serializes it.
      /// Throws FileNotFoundException if snapshot is missing.
      /// </summary>
      public Snapshot DeserializeFromDisk(int pid)
      {
         string filename = String.Format("mrHelper.snapshot.{0}.json", pid);
         string fullSnapshotName = System.IO.Path.Combine(snapshotPath, filename);
         if (!System.IO.File.Exists(fullSnapshotName))
         {
            throw new System.IO.FileNotFoundException(
               String.Format("Cannot find interprocess snapshot at path \"{0}\"", fullSnapshotName));
         }

         return JsonUtils.LoadFromFile<Snapshot>(fullSnapshotName);
      }

      /// <summary>
      /// Erases snapshots from disk.
      /// </summary>
      public static void CleanUpSnapshots()
      {
         if (!System.IO.Directory.Exists(snapshotPath))
         {
            return;
         }

         foreach (string f in System.IO.Directory.EnumerateFiles(snapshotPath, "mrHelper.snapshot.*.json"))
         {
            System.IO.File.Delete(f);
         }
      }
   }
}
