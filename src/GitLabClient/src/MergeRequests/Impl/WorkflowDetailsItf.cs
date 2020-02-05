using System;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Client.Types;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.MergeRequests
{
   internal interface IWorkflowDetails
   {
      /// <summary>
      /// Create a copy of object
      /// </summary>
      IWorkflowDetails Clone();

      /// <summary>
      /// Return a list of merge requests by unique project id
      /// </summary>
      IEnumerable<MergeRequest> GetMergeRequests(ProjectKey key);

      /// <summary>
      /// Return a timestamp of the most recent version of a specified merge request
      /// </summary>
      DateTime GetLatestChangeTimestamp(MergeRequestKey mrk);
   }
}

