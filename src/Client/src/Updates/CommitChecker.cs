using System;

namespace mrHelper.Client
{
   /// <summary>
   /// Checks for new commits
   /// </summary>
   public class CommitChecker
   {
      /// <summary>
      /// Binds to the specific MergeRequestDescriptor
      /// </summary>
      public CommitChecker(MergeRequestDescriptor mrd, UpdateOperator updateOperator)
      {
         MergeRequestDescriptor = mrd;
         UpdateOperator = updateOperator;
      }

      /// <summary>
      /// Checkes for commits newer than the given timestamp
      /// </summary>
      bool AreNewCommits(DateTime timestamp)
      {
         List<Versions> versions = updateOperator.GetVersions(MergeRequestDescriptor);
         return versions != null && versions.Count > 0
            && versions[0].Created_At.ToLocalTime() > timestamp;
      }

      private MergeRequestDescriptor MergeRequestDescriptor { get; }
      private UpdateOperator UpdateOperator { get; }
   }
}

