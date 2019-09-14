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
   public class LocalProjectChecker : IInstantProjectChecker
   {
      /// <summary>
      /// Binds to the specific MergeRequestDescriptor
      /// </summary>
      internal LocalProjectChecker(int mergeRequestId, WorkflowDetails details)
      {
         MergeRequestId = mergeRequestId;
         Details = details;
      }

      /// <summary>
      /// 
      /// Throws nothing
      /// </summary>
      public DateTime GetLatestChangeTimestamp()
      {
         int projectId = Details.GetProjectId(MergeRequestId);
         Debug.Assert(projectId != 0);

         DateTime dateTime = DateTime.MinValue;

         List<MergeRequest> mergeRequests = Details.GetMergeRequests(projectId);
         foreach (MergeRequest mergeRequest in mergeRequests)
         {
            DateTime latestChange = Details.GetLatestChangeTimestamp(mergeRequest.Id);
            dateTime = latestChange > dateTime ? latestChange : dateTime;
         }

         return dateTime;
      }

      public override string ToString()
      {
         return String.Format("MergeRequest Id: {0}", MergeRequestId);
      }

      private int MergeRequestId { get; }
      private WorkflowDetails Details { get; }
   }
}

