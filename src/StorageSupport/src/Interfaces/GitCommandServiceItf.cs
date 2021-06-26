using System;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Common.Exceptions;

namespace mrHelper.StorageSupport
{
   public class GitNotAvailableDataException : ExceptionEx
   {
      public GitNotAvailableDataException(Exception ex)
         : base(String.Empty, ex)
      {
      }
   }

   public class DiffToolLaunchException : ExceptionEx
   {
      public DiffToolLaunchException(Exception ex)
         : base(String.Empty, ex)
      {
      }
   }

   public interface IGitCommandService
   {
      IEnumerable<string> ShowRevision(GitShowRevisionArguments arguments);

      IEnumerable<string> ShowDiff(GitDiffArguments arguments);

      Comparison GetComparison(RevisionComparisonArguments arguments);

      int LaunchDiffTool(DiffToolArguments arguments);

      IFileRenameDetector RenameDetector { get; }

      FullContextDiffProvider FullContextDiffProvider { get; }

      GitDiffAnalyzer GitDiffAnalyzer { get; }
   }
}

