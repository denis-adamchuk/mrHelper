﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;

namespace mrHelper.Client.MergeRequests
{
   public interface IMergeRequestProvider
   {
      /// <summary>
      /// Return open merge requests in the given project
      /// </summary>
      IEnumerable<MergeRequest> GetMergeRequests(ProjectKey projectKey);

      /// <summary>
      /// Return currently cached Merge Request by its key or null if nothing is cached
      /// </summary>
      MergeRequest? GetMergeRequest(MergeRequestKey mrk);
   }
}
