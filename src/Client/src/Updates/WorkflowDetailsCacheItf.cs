using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;
using mrHelper.Client.Updates;
using mrHelper.Client.Workflow;
using System.ComponentModel;
using System.Diagnostics;

namespace mrHelper.Client.Updates
{
   internal interface IWorkflowDetailsCache
   {
      List<MergeRequest> GetProjectMergeRequests(int projectId);
      DateTime GetLatestCommitTimestamp(int mergeRequestId);
   }
}

