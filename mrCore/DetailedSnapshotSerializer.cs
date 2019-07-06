using System;
using System.Web.Script.Serialization;

namespace mrCore
{
   public struct InterprocessSnapshot
   {
      public int Id;
      public string Host;
      public string AccessToken;
      public string Project;
      public DiffRefs Refs;
      public string TempFolder;
   }

   public class InterprocessSnapshotSerializer
   {
      private static string snapshotPath = Environment.GetEnvironmentVariable("TEMP");
      private static string InterprocessSnapshotFileName = "snapshot.json";

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
         snapshot.Id = json["Id"];
         snapshot.Refs.BaseSHA = json["BaseSHA"];
         snapshot.Refs.StartSHA = json["StartSHA"];
         snapshot.Refs.HeadSHA = json["HeadSHA"];
         snapshot.TempFolder = json["TempFolder"];
         return snapshot;
      }

      public void PurgeSerialized()
      {
         System.IO.File.Delete(System.IO.Path.Combine(snapshotPath, InterprocessSnapshotFileName));
      }
   }
}
