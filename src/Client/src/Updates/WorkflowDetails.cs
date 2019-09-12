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
   internal struct WorkflowDetails
   {
      // maps unique project id to list of merge requests
      public Dictionary<int, List<MergeRequest>> MergeRequests;

      // maps unique Merge Request Id (not IId) to a timestamp of its latest commit
      public Dictionary<int, DateTime> Commits;
   }
}

