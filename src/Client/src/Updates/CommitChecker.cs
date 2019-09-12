using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;
using mrHelper.Client.Git;

namespace mrHelper.Client.Updates
{
   /// <summary>
   /// Checks for new commits
   /// </summary>
   public class CommitChecker
   {
      /// <summary>
      /// Binds to the specific MergeRequestDescriptor
      /// </summary>
      internal CommitChecker(int mergeRequestId, WorkflowDetails details)
      {
         MergeRequestId = mergeRequestId;
         Details = details;
      }

      /// <summary>
      /// Check for commits newer than the given timestamp
      /// Throws nothing
      /// </summary>
      public DateTime GetLatestCommitTimestamp()
      {
         return Details.Commits[MergeRequestId];
      }

      public override string ToString()
      {
         return String.Format("MergeRequest Id: {0}", MergeRequestId);
      }

      private int MergeRequestId { get; }
      private IWorkflowDetailsCache DetailsCache { get; }
   }
}

