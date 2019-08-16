﻿using System;
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
   /// Checks for new commits
   /// </summary>
   public class CommitChecker
   {
      /// <summary>
      /// Binds to the specific MergeRequestDescriptor
      /// </summary>
      internal CommitChecker(MergeRequestDescriptor mrd, UpdateOperator updateOperator)
      {
         MergeRequestDescriptor = mrd;
         UpdateOperator = updateOperator;
      }

      /// <summary>
      /// Check for commits newer than the given timestamp
      /// </summary>
      async public Task<bool> AreNewCommitsAsync(DateTime timestamp)
      {
         List<GitLabSharp.Entities.Version> versions = await UpdateOperator.GetVersions(MergeRequestDescriptor);
         return versions != null && versions.Count > 0 && versions[0].Created_At.ToLocalTime() > timestamp;
      }

      private MergeRequestDescriptor MergeRequestDescriptor { get; }
      private UpdateOperator UpdateOperator { get; }
   }
}
