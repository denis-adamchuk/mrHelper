using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;
using mrHelper.Client.Git;
using Version = GitLabSharp.Entities.Version;

namespace mrHelper.Client.Updates
{
   /// <summary>
   /// Detects the latest change in a merge request by means of a request to GitLab
   /// </summary>
   public class RemoteProjectChecker : IInstantProjectChecker
   {
      internal RemoteProjectChecker(MergeRequestDescriptor mrd, UpdateOperator updateOperator)
      {
         MergeRequestDescriptor = mrd;
         Operator = updateOperator;
      }

      /// <summary>
      /// Get a timestamp of the most recent change of a project the merge request belongs to
      /// Throws nothing
      /// </summary>
      public DateTime GetLatestChangeTimestamp()
      {
         try
         {
            Task<Version> task = Task.Run<Version>(
               async () => await Operator.GetLatestVersionAsync(MergeRequestDescriptor));
            return task.Result.Created_At;
         }
         catch (OperatorException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot check for commits");
         }
         return DateTime.MinValue;
      }

      public override string ToString()
      {
         return String.Format("MRD: HostName={0}, ProjectName={1}, IId={2}",
            MergeRequestDescriptor.HostName, MergeRequestDescriptor.ProjectName, MergeRequestDescriptor.IId);
      }

      private MergeRequestDescriptor MergeRequestDescriptor { get; }
      private UpdateOperator Operator { get; }
   }
}

