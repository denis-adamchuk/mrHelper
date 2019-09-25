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
      async public Task<DateTime> GetLatestChangeTimestampAsync()
      {
         DateTime dateTime = DateTime.MinValue;
         try
         {
            Version version = await Operator.GetLatestVersionAsync(MergeRequestDescriptor);
            dateTime = version.Created_At;
         }
         catch (OperatorException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot check for commits");
         }
         return dateTime;
      }

      public override string ToString()
      {
         return String.Format("RemoteProjectChecker. MRD: HostName={0}, ProjectName={1}, IId={2}",
            MergeRequestDescriptor.HostName, MergeRequestDescriptor.ProjectName, MergeRequestDescriptor.IId);
      }

      private MergeRequestDescriptor MergeRequestDescriptor { get; }
      private UpdateOperator Operator { get; }
   }
}

