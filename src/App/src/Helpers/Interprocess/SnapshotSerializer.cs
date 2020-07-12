using System;

namespace mrHelper.App.Interprocess
{
   /// <summary>
   /// Encapsulates work with the interprocess snapshot.
   /// </summary>
   public class SnapshotSerializer
   {
      private static readonly string snapshotPath = Environment.GetEnvironmentVariable("TEMP");

      /// <summary>
      /// Serializes snapshot to disk.
      /// </summary>
      public void SerializeToDisk(Snapshot snapshot, int pid)
      {
         string filename = String.Format("mrHelper.snapshot.{0}.json", pid);
         Common.Tools.JsonUtils.SaveToFile(System.IO.Path.Combine(snapshotPath, filename), snapshot);
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

         return Common.Tools.JsonUtils.LoadFromFile<Snapshot>(fullSnapshotName);
      }

      /// <summary>
      /// Erases snapshots from disk.
      /// </summary>
      public static void CleanUpSnapshots()
      {
         foreach (string f in System.IO.Directory.EnumerateFiles(snapshotPath, "mrHelper.snapshot.*.json"))
         {
            System.IO.File.Delete(f);
         }
      }
   }
}
