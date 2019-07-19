using System;
using System.Web.Script.Serialization;

namespace mrCore
{
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

      public InterprocessSnapshot? DeserializeFromDisk()
      {
         string fullSnapshotName = System.IO.Path.Combine(snapshotPath,InterprocessSnapshotFileName);
         if (!System.IO.File.Exists(fullSnapshotName))
         {
            return null;
         }

         string jsonStr = System.IO.File.ReadAllText(fullSnapshotName);

         JavaScriptSerializer serializer = new JavaScriptSerializer();
         dynamic json = serializer.DeserializeObject(jsonStr);

         InterprocessSnapshot snapshot;
         snapshot.Host = json["Host"];
         snapshot.AccessToken = json["AccessToken"];
         snapshot.Project = json["Project"];
         snapshot.MergeRequestId = json["MergeRequestId"];
         snapshot.TempFolder = json["TempFolder"];
         dynamic refs = json["Refs"];
         snapshot.Refs.LeftSHA = refs["LeftSHA"];
         snapshot.Refs.RightSHA = refs["RightSHA"];
         return snapshot;
      }

      public void PurgeSerialized()
      {
         System.IO.File.Delete(System.IO.Path.Combine(snapshotPath, InterprocessSnapshotFileName));
      }
   }
}
