﻿using System;

namespace mrHelper.GitLabClient
{
   internal interface IModificationListener
   {
      void OnMergeRequestModified(MergeRequestKey mergeRequestKey);

      void OnDiscussionResolved(MergeRequestKey mergeRequestKey);

      void OnTrackedTimeModified(MergeRequestKey mergeRequestKey, TimeSpan span, bool add);
   }
}

