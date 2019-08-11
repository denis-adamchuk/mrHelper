using System;

namespace mrHelper.Client
{
   public class CommitChecker
   {
      public CommitChecker(MergeRequestDescriptor mrd, UpdateOperator updateOperator)
      {
         MergeRequestDescriptor = mrd;
         UpdateOperator = updateOperator;
      }

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

