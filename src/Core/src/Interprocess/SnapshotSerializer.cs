using System;
using System.Web.Script.Serialization;

namespace mrHelper.Core.Interprocess
{
   /// <summary>
   /// Encapsulates work with the interprocess snapshot.
   /// </summary>
   public class SnapshotSerializer
   {
      private static readonly string snapshotPath = Environment.GetEnvironmentVariable("TEMP");
      private static readonly string SnapshotFileName = "snapshot.json";

      /// <summary>
      /// Serializes snapshot to disk.
      /// </summary>
      public void SerializeToDisk(Snapshot snapshot)
      {
         JavaScriptSerializer serializer = new JavaScriptSerializer();
         string json = serializer.Serialize(snapshot);
         System.IO.File.WriteAllText(System.IO.Path.Combine(snapshotPath, SnapshotFileName), json);
      }

      /// <summary>
      /// Loads snapshot from disk and de-serializes it.
      /// Throws FileNotFoundException if snapshot is missing.
      /// </summary>
      public Snapshot DeserializeFromDisk()
      {
         string fullSnapshotName = System.IO.Path.Combine(snapshotPath, SnapshotFileName);
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
      /// Erases snapshot from disk.
      /// </summary>
      public void PurgeSerialized()
      {
         System.IO.File.Delete(System.IO.Path.Combine(snapshotPath, SnapshotFileName));
      }
   }
}
