﻿using System;
using System.Web.Script.Serialization;

namespace mrCore
{
   public struct MergeRequestDetails
   {
      public int Id;
      public string Host;
      public string AccessToken;
      public string Project;
      public DiffRefs Refs;
      public string TempFolder;
   }

   public class DetailedSnapshotSerializer
   {
      private static string snapshotPath = Environment.GetEnvironmentVariable("TEMP");
      private static string InterprocessSnapshotFilename = "details.json";

      public void SerializeToDisk(MergeRequestDetails details)
      {
         JavaScriptSerializer serializer = new JavaScriptSerializer();
         string json = serializer.Serialize(details);
         System.IO.File.WriteAllText(System.IO.Path.Combine(snapshotPath, InterprocessSnapshotFilename), json);
      }

      public MergeRequestDetails? DeserializeFromDisk()
      {
         string fullSnapshotName = System.IO.Path.Combine(snapshotPath,InterprocessSnapshotFilename);
         if (!System.IO.File.Exists(fullSnapshotName))
         {
            return null;
         }

         string jsonStr = System.IO.File.ReadAllText(fullSnapshotName);

         JavaScriptSerializer serializer = new JavaScriptSerializer();
         dynamic json = serializer.DeserializeObject(jsonStr);

         MergeRequestDetails details;
         details.Host = json["Host"];
         details.AccessToken = json["AccessToken"];
         details.Project = json["Project"];
         details.Id = json["Id"];
         details.Refs.BaseSHA = json["BaseSHA"];
         details.Refs.StartSHA = json["StartSHA"];
         details.Refs.HeadSHA = json["HeadSHA"];
         details.TempFolder = json["TempFolder"];
         return details;
      }

      public void PurgeSerialized()
      {
         System.IO.File.Delete(System.IO.Path.Combine(snapshotPath, InterprocessSnapshotFilename));
      }
   }
}
