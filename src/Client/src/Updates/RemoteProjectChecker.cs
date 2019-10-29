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
      internal RemoteProjectChecker(MergeRequestKey mrk, UpdateOperator updateOperator)
      {
         _mergeRequestKey = mrk;
         _operator = updateOperator;
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
            Version version = await _operator.GetLatestVersionAsync(_mergeRequestKey);
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
         return String.Format("RemoteProjectChecker. MRK: HostName={0}, ProjectName={1}, IId={2}",
            _mergeRequestKey.ProjectKey.HostName, _mergeRequestKey.ProjectKey.ProjectName, _mergeRequestKey.IId);
      }

      private MergeRequestKey _mergeRequestKey;
      private readonly UpdateOperator _operator;
   }
}

