using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace mrCore
{
   public class GitDiffAnalyzer
   {
      static Regex diffSectionRe = new Regex(
         @"\@\@\s-(?'left_start'\d+)(,(?'left_len'\d+))?\s\+(?'right_start'\d+)(,(?'right_len'\d+))?\s\@\@",
         RegexOptions.Compiled);
      struct GitDiffSection
      {
         public int LeftSectionStart;
         public int LeftSectionEnd;
         public int RightSectionStart;
         public int RightSectionEnd;
      }

      public GitDiffAnalyzer(string sha1, string sha2, string filename)
      {
         _sections = getDiffSections(sha1, sha2, filename);
      }

      public bool IsLineAddedOrModified(int linenumber)
      {
         foreach (var section in _sections)
         {
            if (linenumber >= section.RightSectionStart && linenumber < section.RightSectionEnd)
            {
               return true;
            }
         }
         return false;
      }

      public bool IsLineDeleted(int linenumber)
      {
         foreach (var section in _sections)
         {
            if (linenumber >= section.LeftSectionStart && linenumber < section.LeftSectionEnd)
            {
               return true;
            }
         }
         return false;
      }

      static private List<GitDiffSection> getDiffSections(string sha1, string sha2, string filename)
      {
         List<GitDiffSection> sections = new List<GitDiffSection>();

         List<string> diff = GitClient.Diff(sha1, sha2, filename);
         foreach (string line in diff)
         {
            Match m = diffSectionRe.Match(line);
            if (!m.Success || m.Groups.Count < 3)
            {
               continue;
            }

            if (!m.Groups["left_start"].Success || !m.Groups["right_start"].Success)
            {
               continue;
            }

            // @@ -1 +1 @@ is essentially the same as @@ -1,1 +1,1 @@
            int leftSectionLength = m.Groups["left_len"].Success ? int.Parse(m.Groups["left_len"].Value) : 1;
            int rightSectionLength = m.Groups["right_len"].Success ? int.Parse(m.Groups["right_len"].Value) : 1;

            GitDiffSection section;
            section.LeftSectionStart = int.Parse(m.Groups["left_start"].Value);
            section.LeftSectionEnd = section.LeftSectionStart + leftSectionLength;
            section.RightSectionStart = int.Parse(m.Groups["right_start"].Value);
            section.RightSectionEnd = section.RightSectionStart + rightSectionLength;
            sections.Add(section);
         }

         return sections;
      }

      private readonly List<GitDiffSection> _sections;
   }
}
