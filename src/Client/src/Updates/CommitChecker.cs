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
      async public Task<Commit> GetLatestCommitAsync()
      {
         try
         {
            return await UpdateOperator.GetLatestCommitAsync(MergeRequestDescriptor);
         }
         catch (OperatorException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot check for commits");
         }
         return new Commit();
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

