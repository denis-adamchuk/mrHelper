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
      internal CommitChecker(MergeRequestDescriptor mrd, UpdateOperator updateOperator)
      {
         MergeRequestDescriptor = mrd;
         UpdateOperator = updateOperator;
      }

      /// <summary>
      /// Check for commits newer than the given timestamp
      /// Throws nothing
      /// </summary>
      async public Task<bool> AreNewCommitsAsync(DateTime timestamp)
      {
         List<GitLabSharp.Entities.Version> versions = null;
         try
         {
            versions = await UpdateOperator.GetVersions(MergeRequestDescriptor);
         }
         catch (OperatorException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot check for versions");
            return false;
         }
         return versions != null && versions.Count > 0 && versions[0].Created_At.ToLocalTime() > timestamp;
      }

      public override string ToString()
      {
         return String.Format("MRD: HostName={0}, ProjectName={1}, IId={2}",
            MergeRequestDescriptor.HostName, MergeRequestDescriptor.ProjectName, MergeRequestDescriptor.IId);
      }

      private MergeRequestDescriptor MergeRequestDescriptor { get; }
      private UpdateOperator UpdateOperator { get; }
   }
}

