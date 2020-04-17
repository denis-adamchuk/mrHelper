using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using mrHelper.Client.Types;
using mrHelper.Common.Interfaces;
using Version = GitLabSharp.Entities.Version;

namespace mrHelper.Client.MergeRequests
{
   /// <summary>
   /// Detects the latest change in a merge request using Local cache only
   /// </summary>
   public class LocalProjectChecker : IInstantProjectChecker
   {
      internal LocalProjectChecker(IEnumerable<Version> versions)
      {
         _versions = versions;
      }

      /// <summary>
      /// Get a timestamp of the most recent change of a project the merge request belongs to
      /// Throws nothing
      /// </summary>
      public Task<ProjectSnapshot> GetProjectSnapshot()
      {
         List<string> shas = new List<string>();
         foreach (Version version in _versions)
         {
            shas.Add(version.Base_Commit_SHA);
            shas.Add(version.Head_Commit_SHA);
         }

         ProjectSnapshot projectSnapshot = new ProjectSnapshot
         {
            LatestChange = _versions.OrderBy(x => x.Created_At).LastOrDefault().Created_At,
            Sha = shas
         };
         return Task.FromResult(projectSnapshot);
      }

      public override string ToString()
      {
         return String.Format("LocalProjectChecker. Version Count: {0}", _versions.Count());
      }

      private readonly IEnumerable<Version> _versions;
   }
}

