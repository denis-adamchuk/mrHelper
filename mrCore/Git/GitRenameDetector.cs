using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace mrCore
{
   public class GitRenameDetector
   {
      static Regex diffRenameRe = new Regex(
         @"\d+\s+\d+\s+(?'left_name'.+)\s\=\>\s(?'right_name'.+)", RegexOptions.Compiled);

      public GitRenameDetector(GitRepository gitRepository)
      {
         _gitRepository = gitRepository;
      }

      // returns a name of file at the opposite side. if not renamed, returns 'filename'
      public string IsRenamed(string leftcommit, string rightcommit, string filename, bool leftsidename)
      {
         List<string> renames = _gitRepository.GetListOfRenames(leftcommit, rightcommit);

         foreach (string line in renames)
         {
            Match m = diffRenameRe.Match(line);
            if (!m.Success || m.Groups.Count < 2)
            {
               continue;
            }

            if (!m.Groups["left_name"].Success || !m.Groups["right_name"].Success)
            {
               continue;
            }

            if (leftsidename && m.Groups["left_name"].Value == filename)
            {
               return m.Groups["right_name"].Value;
            }
            else if (!leftsidename && m.Groups["right_name"].Value == filename)
            {
               return m.Groups["left_name"].Value;
            }
         }

         return filename;
      }

      private readonly GitRepository _gitRepository;
   }
}
