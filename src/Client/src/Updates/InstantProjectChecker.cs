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
   /// Checks for changes in GitLab projects
   /// </summary>
   public class InstantProjectChecker
   {
      /// <summary>
      /// Binds to the specific MergeRequestDescriptor
      /// </summary>
      internal InstantProjectChecker(int mergeRequestId, WorkflowDetails details)
      {
         MergeRequestId = mergeRequestId;
         Details = details;
      }

      /// <summary>
      /// Get a timestamp of the most recent change of a project the merge request belongs to
      /// Throws nothing
      /// </summary>
      public DateTime GetLatestChangeTimestamp()
      {
         return Details.GetLatestChangeTimestamp(MergeRequestId);
      }

      public override string ToString()
      {
         return String.Format("MergeRequest Id: {0}", MergeRequestId);
      }

      private int MergeRequestId { get; }
      private WorkflowDetails Details { get; }
   }
}

