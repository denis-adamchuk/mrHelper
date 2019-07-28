using System;
using System.Web.Script.Serialization;

namespace mrCore
{
   /// <summary>
   /// Data structure used for communication between the main application instance and instances launched from diff tool
   /// </summary>
   public struct InterprocessSnapshot
   {
      public int MergeRequestId;
      public string Host;
      public string AccessToken;
      public string Project;
      public DiffRefs Refs;
      public string TempFolder;
   }

   public class InterprocessSnapshotSerializer
   {
      private static readonly string snapshotPath = Environment.GetEnvironmentVariable("TEMP");
      private static readonly string InterprocessSnapshotFileName = "snapshot.json";

      public void SerializeToDisk(InterprocessSnapshot snapshot)
      {
         JavaScriptSerializer serializer = new JavaScriptSerializer();
         string json = serializer.Serialize(snapshot);
         System.IO.File.WriteAllText(System.IO.Path.Combine(snapshotPath, InterprocessSnapshotFileName), json);
      }

      public InterprocessSnapshot DeserializeFromDisk()
      {
         string fullSnapshotName = System.IO.Path.Combine(snapshotPath, InterprocessSnapshotFileName);
         if (!System.IO.File.Exists(fullSnapshotName))
         {
            throw new IOException(
               String.Empty("Cannot find interprocess snapshot at path \"{0}\"", fullSnapshotName));
         }

         string jsonStr = System.IO.File.ReadAllText(fullSnapshotName);

         JavaScriptSerializer serializer = new JavaScriptSerializer();
         return serializer.Deserialize<InterprocessSnapshot>(jsonStr);
      }

      public void PurgeSerialized()
      {
         System.IO.File.Delete(System.IO.Path.Combine(snapshotPath, InterprocessSnapshotFileName));
      }
   }
}
