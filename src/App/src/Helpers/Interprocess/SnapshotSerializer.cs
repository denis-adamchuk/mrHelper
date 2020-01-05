using System;
using System.Web.Script.Serialization;

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
         JavaScriptSerializer serializer = new JavaScriptSerializer();
         string json = serializer.Serialize(snapshot);
         string filename = String.Format("mrHelper.snapshot.{0}.json", pid);
         System.IO.File.WriteAllText(System.IO.Path.Combine(snapshotPath, filename), json);
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

         string jsonStr = System.IO.File.ReadAllText(fullSnapshotName);

         JavaScriptSerializer serializer = new JavaScriptSerializer();
         return serializer.Deserialize<Snapshot>(jsonStr);
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
