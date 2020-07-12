using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using Version = GitLabSharp.Entities.Version;
using mrHelper.StorageSupport;

namespace mrHelper.App.Helpers
{
   /// <summary>
   /// Detects the latest change among given versions
   /// </summary>
   internal class VersionBasedContextProvider : ICommitStorageUpdateContextProvider
   {
      internal VersionBasedContextProvider(IEnumerable<Version> versions)
      {
         _versions = versions;
      }

      public CommitStorageUpdateContext GetContext()
      {
         if (_versions == null)
         {
            Debug.Assert(false);
            return null;
         }

         if (!_versions.Any())
         {
            return null;
         }

         Dictionary<string, HashSet<string>> baseToHeads = new Dictionary<string, HashSet<string>>();
         foreach (Version version in _versions)
         {
            string baseSha = version.Base_Commit_SHA;
            string headSha = version.Head_Commit_SHA;
            if (baseSha != null && headSha != null && baseSha != headSha)
            {
               if (!baseToHeads.ContainsKey(baseSha))
               {
                  baseToHeads[baseSha] = new HashSet<string>();
               }
               baseToHeads[baseSha].Add(headSha);
            }
         }

         Version latestVersion = _versions.OrderBy(x => x.Created_At).LastOrDefault();
         return new FullUpdateContext(latestVersion.Created_At,
            baseToHeads.ToDictionary(item => item.Key, item => item.Value.AsEnumerable()));
      }

      public override string ToString()
      {
         return String.Format("VersionBasedContextProvider. Version Count: {0}", _versions.Count());
      }

      private readonly IEnumerable<Version> _versions;
   }
}

