using System;
using System.Linq;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.StorageSupport;
using static mrHelper.StorageSupport.BaseToHeadsCollection;

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

         Dictionary<string, Dictionary<string, HashSet<RelativeFileInfo>>> baseToHeads =
            new Dictionary<string, Dictionary<string, HashSet<RelativeFileInfo>>>();
         foreach (Discussion diffNote in diffNotes)
         {
            string baseSha = diffNote.Notes.First().Position.Base_SHA;
            string headSha = diffNote.Notes.First().Position.Head_SHA;
            if (baseSha != null && headSha != null && baseSha != headSha)
            {
               if (!baseToHeads.ContainsKey(baseSha))
               {
                  baseToHeads[baseSha] = new Dictionary<string, HashSet<RelativeFileInfo>>();
               }

               if (!baseToHeads[baseSha].ContainsKey(headSha))
               {
                  baseToHeads[baseSha][headSha] = new HashSet<RelativeFileInfo>();
               }

               string oldPath = diffNote.Notes.First().Position.Old_Path;
               string newPath = diffNote.Notes.First().Position.New_Path;
               baseToHeads[baseSha][headSha].Add(new RelativeFileInfo(oldPath, newPath));
            }
         }

         return new PartialUpdateContext(
            new BaseToHeadsCollection(
               baseToHeads.ToDictionary(
                  item => new CommitInfo(item.Key),
                  item => item.Value.Select(x => new RelativeCommitInfo(x.Key, x.Value)))));
      }

      public override string ToString()
      {
         return String.Format("DiscussionBasedContextProvider. Discussion Count: {0}", _discussions.Count());
      }

      private readonly IEnumerable<Discussion> _discussions;
   }
}

