using mrHelper.Core.Matching;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace mrHelper.App.Interprocess
{
   /// <summary>
   /// Data structure used for communication between the main application instance and instances launched from diff tool
   /// </summary>
   public class Snapshot
   {
      public Snapshot(int mergeRequestIId, string host, string accessToken, string project,
         DiffRefs refs, string tempFolder, string sessionName)
      {
         MergeRequestIId = mergeRequestIId;
         Host = host;
         AccessToken = accessToken;
         Project = project;
         Refs = refs;
         TempFolder = tempFolder;
         DataCacheName = sessionName;
      }

      [JsonProperty]
      public int MergeRequestIId { get; protected set; }

      [JsonProperty]
      public string Host { get; protected set; }

      [JsonProperty]
      public string AccessToken { get; protected set; }

      [JsonProperty]
      public string Project { get; protected set; }

      [JsonProperty]
      public DiffRefs Refs { get; protected set; }

      [JsonProperty]
      public string TempFolder { get; protected set; }

      [JsonProperty]
      public string DataCacheName { get; protected set; }

      public override bool Equals(object obj)
      {
         return obj is Snapshot snapshot &&
                MergeRequestIId == snapshot.MergeRequestIId &&
                Host == snapshot.Host &&
                AccessToken == snapshot.AccessToken &&
                Project == snapshot.Project &&
                EqualityComparer<DiffRefs>.Default.Equals(Refs, snapshot.Refs) &&
                TempFolder == snapshot.TempFolder &&
                DataCacheName == snapshot.DataCacheName;
      }

      public override int GetHashCode()
      {
         int hashCode = -434553603;
         hashCode = hashCode * -1521134295 + MergeRequestIId.GetHashCode();
         hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Host);
         hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(AccessToken);
         hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Project);
         hashCode = hashCode * -1521134295 + EqualityComparer<DiffRefs>.Default.GetHashCode(Refs);
         hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TempFolder);
         hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(DataCacheName);
         return hashCode;
      }
   }
}
