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
         return new PartialUpdateContext(
            Enumerable.Concat(
               diffNotes
               .Select(x => x.Notes.First().Position.Base_SHA).Distinct(),
               diffNotes
               .Select(x => x.Notes.First().Position.Head_SHA).Distinct()));
      }

      public override string ToString()
      {
         return String.Format("DiscussionBasedContextProvider. Discussion Count: {0}", _discussions.Count());
      }

      private readonly IEnumerable<Discussion> _discussions;
   }
}

