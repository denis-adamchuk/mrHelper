using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using mrHelper.Common.Interfaces;

namespace mrHelper.Core.Git
{
   /// <summary>
   /// Detects if a file was renamed between two commits. Uses default git threshold.
   /// </summary>
   public class GitRenameDetector
   {
      private static readonly Regex diffRenameRe = new Regex(
         @"(?'added'\d+)\s+(?'deleted'\d+)\s+(?'left_name'.+)\s\=\>\s(?'right_name'.+)", RegexOptions.Compiled);

      public GitRenameDetector(IGitRepository gitRepository)
      {
         _gitRepository = gitRepository;
      }

      /// <summary>
      /// Returns a name of file at the opposite side.
      /// Throws GitOperationException in case of problems with git.
      /// </summary>
      public string IsRenamed(string leftcommit, string rightcommit, string filename, bool leftsidename, out bool moved)
      {
         GitListOfRenamesArguments arguments = new GitListOfRenamesArguments
         {
            sha1 = leftcommit,
            sha2 = rightcommit
         };

         IEnumerable<string> renames = _gitRepository.GetListOfRenames(arguments);
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

      private readonly IGitRepository _gitRepository;
   }
}
