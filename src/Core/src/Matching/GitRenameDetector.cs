using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using mrHelper.Common.Interfaces;
using mrHelper.Core.Matching;

namespace mrHelper.Core.Git
{
   /// <summary>
   /// Detects if a file was renamed between two commits. Uses default git threshold.
   /// </summary>
   public class GitRenameDetector
   {
      private static readonly Regex diffRenameRe = new Regex(
         @"(?'added'\d+)\s+(?'deleted'\d+)\s+(?'left_name'.+)\s\=\>\s(?'right_name'.+)", RegexOptions.Compiled);

      public GitRenameDetector(IGitCommitStorage gitRepository)
      {
         _gitRepository = gitRepository;
      }

      /// <summary>
      /// Returns a name of file at the opposite side.
      /// Throws MatchingException.
      /// </summary>
      public string IsRenamed(string leftcommit, string rightcommit, string filename, bool leftsidename, out bool moved)
      {
         GitDiffArguments arguments = new GitDiffArguments(
            GitDiffArguments.DiffMode.NumStat,
            new GitDiffArguments.CommonArguments(leftcommit, rightcommit, null, null, "R"),
            null);

         IEnumerable<string> renames;
         try
         {
            renames = _gitRepository.Data?.Get(arguments);
         }
         catch (GitNotAvailableDataException ex)
         {
            throw new MatchingException("Cannot obtain list of renamed files", ex);
         }

         if (renames == null)
         {
            throw new MatchingException("Cannot obtain list of renamed files", null);
         }

         moved = false;

         foreach (string line in renames)
         {
            Match m = diffRenameRe.Match(line);
            if (!m.Success || m.Groups.Count < 4)
            {
               continue;
            }

            if (!m.Groups["left_name"].Success || !m.Groups["right_name"].Success)
            {
               continue;
            }

            Debug.Assert(m.Groups["added"].Success);
            Debug.Assert(m.Groups["deleted"].Success);

            int added = int.Parse(m.Groups["added"].Value);
            int deleted = int.Parse(m.Groups["deleted"].Value);

            string leftName = m.Groups["left_name"].Value;
            string rightName = m.Groups["right_name"].Value;

            if (leftName.Contains('{'))
            {
               Debug.Assert(rightName.Contains('}'));

               int leftPathIdx = leftName.IndexOf('{');
               string commonPrefix = leftName.Substring(0, leftPathIdx);
               string leftPart = leftName.Substring(leftPathIdx + 1, leftName.Length - leftPathIdx - 1);

               int rightPathIdx = rightName.IndexOf('}');
               string commonSuffix = rightName.Substring(rightPathIdx + 1, rightName.Length - rightPathIdx - 1);
               string rightPart = rightName.Substring(0, rightPathIdx);

               leftName = commonPrefix + leftPart + commonSuffix;
               rightName = commonPrefix + rightPart + commonSuffix;
            }

            if (leftsidename && leftName == filename)
            {
               moved = (added + deleted == 0);
               return rightName;
            }
            else if (!leftsidename && rightName == filename)
            {
               moved = (added + deleted == 0);
               return leftName;
            }
         }

         return filename;
      }

      private readonly IGitCommitStorage _gitRepository;
   }
}
