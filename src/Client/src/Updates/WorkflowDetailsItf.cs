using System;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;

namespace mrHelper.Client.Updates
{
   internal interface IWorkflowDetails
   {
      /// <summary>
      /// Create a copy of object
      /// </summary>
      IWorkflowDetails Clone();

      /// <summary>
      /// Return project name (Path_With_Namespace) by hostname and unique project Id
      /// </summary>
      string GetProjectName(OldProjectKey key);

      /// <summary>
      /// Return a list of merge requests by unique project id
      /// </summary>
      List<MergeRequest> GetMergeRequests(ProjectKey key);

      /// <summary>
      /// Return a timestamp of the most recent version of a specified merge request
      /// </summary>
      DateTime GetLatestChangeTimestamp(MergeRequestKey mrk);
   }
}

