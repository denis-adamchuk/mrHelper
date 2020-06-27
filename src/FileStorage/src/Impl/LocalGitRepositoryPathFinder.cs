using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using mrHelper.Client.Types;
using mrHelper.Common.Interfaces;

namespace mrHelper.FileStorage
{
   public static class FileStoragePathFinder
   {
      /// <summary>
      /// Returns either a path to empty folder or a path to a folder with already cloned repository
      /// </summary>
      public static string FindPath(string parentFolder, MergeRequestKey mrk)
      {
         Trace.TraceInformation(String.Format(
            "[FileStoragePathFinder] Searching for a path for MR with IId {0} of project {1} in \"{2}\"...",
            mrk.IId, mrk.ProjectKey.ProjectName, parentFolder));

         throw new NotImplementedException();
      }
   }
}

