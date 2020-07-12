using System;
using System.Linq;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.StorageSupport;

namespace mrHelper.App.Helpers
{
   internal class DiscussionBasedContextProvider : ICommitStorageUpdateContextProvider
   {
      internal DiscussionBasedContextProvider(IEnumerable<Discussion> discussions)
      {
         _discussions = discussions;
      }

      public CommitStorageUpdateContext GetContext()
      {
         IEnumerable<Discussion> diffNotes =
               _discussions
               .Where(x => x.Notes != null && x.Notes.Any() && x.Notes.First().Type == "DiffNote");

         Dictionary<string, HashSet<string>> baseToHeads = new Dictionary<string, HashSet<string>>();
         foreach (Discussion diffNote in diffNotes)
         {
            string baseSha = diffNote.Notes.First().Position.Base_SHA;
            string headSha = diffNote.Notes.First().Position.Head_SHA;
            if (baseSha != null && headSha != null && baseSha != headSha)
            {
               if (!baseToHeads.ContainsKey(baseSha))
               {
                  baseToHeads[baseSha] = new HashSet<string>();
               }
               baseToHeads[baseSha].Add(headSha);
            }
         }

         return new PartialUpdateContext(baseToHeads.ToDictionary(item => item.Key, item => item.Value.AsEnumerable()));
      }

      public override string ToString()
      {
         return String.Format("DiscussionBasedContextProvider. Discussion Count: {0}", _discussions.Count());
      }

      private readonly IEnumerable<Discussion> _discussions;
   }
}

