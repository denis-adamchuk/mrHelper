using System;

namespace mrHelper.Core.Interprocess
{
   /// <summary>
   /// Data structure used for communication between the main application instance and instances launched from diff tool
   /// </summary>
   public struct Snapshot
   {
      public int MergeRequestIId;
      public string Host;
      public string AccessToken;
      public string Project;
      public DiffRefs Refs;
      public string TempFolder;
   }
}

