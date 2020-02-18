using System;
using System.Threading.Tasks;
using mrHelper.Client.Types;
using mrHelper.Client.Versions;
using mrHelper.Common.Interfaces;
using Version = GitLabSharp.Entities.Version;

namespace mrHelper.Client.MergeRequests
{
   /// <summary>
   /// Detects the latest change in a merge request using Local cache only
   /// </summary>
   public class LocalProjectChecker : IInstantProjectChecker
   {
      internal LocalProjectChecker(MergeRequestKey mrk, IWorkflowDetails details)
      {
         _mergeRequestKey = mrk;
         _details = details;
      }

      /// <summary>
      /// Get a timestamp of the most recent change of a project the merge request belongs to
      /// Throws nothing
      /// </summary>
      public Task<DateTime> GetLatestChangeTimestamp()
      {
         return Task.FromResult(_details.GetLatestVersion(_mergeRequestKey).Created_At);

         /*
            Commented out: advanced algorithm of detecting the most latest timestamp
            It optimizes things in some cases but in case of big number of MRs it may become inefficient
            and cause often `git fetch` calls. May be it will be optimized and uncommented later.

            int projectId = Details.GetProjectId(MergeRequestId);
            Debug.Assert(projectId != 0);

            DateTime dateTime = DateTime.MinValue;

            IEnumerable<MergeRequest> mergeRequests = Details.GetMergeRequests(projectId);
            foreach (MergeRequest mergeRequest in mergeRequests)
            {
               DateTime latestChange = Details.GetLatestChangeTimestamp(mergeRequest.Id);
               dateTime = latestChange > dateTime ? latestChange : dateTime;
            }

            return dateTime;
         */
      }

      public override string ToString()
      {
         return String.Format("LocalProjectChecker. MergeRequest IId: {0}", _mergeRequestKey.IId);
      }

      private MergeRequestKey _mergeRequestKey;
      private readonly IWorkflowDetails _details;
   }
}

