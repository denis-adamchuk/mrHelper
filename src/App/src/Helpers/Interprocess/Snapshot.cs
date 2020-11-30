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
         DiffRefs refs, string tempFolder, string dataCacheName, int dataCacheHashCode)
      {
         MergeRequestIId = mergeRequestIId;
         Host = host;
         AccessToken = accessToken;
         Project = project;
         Refs = refs;
         TempFolder = tempFolder;
         DataCacheName = dataCacheName;
         DataCacheHashCode = dataCacheHashCode;
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

      [JsonProperty]
      public int DataCacheHashCode { get; protected set; }
   }
}

