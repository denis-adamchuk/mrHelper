using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.Core.Context;

namespace mrHelper.Core.Git
{
   /// <summary>
   /// Checks whether a line number belongs to added/modified or deleted sections (or none of them)
   /// </summary>
   public class GitDiffAnalyzer
   {
      private static readonly Regex diffSectionRe = new Regex(
         @"^\@\@\s-(?'left_start'\d+)(,(?'left_len'\d+))?\s\+(?'right_start'\d+)(,(?'right_len'\d+))?\s\@\@",
         RegexOptions.Compiled);

      private struct GitDiffSection
      {
         public int LeftSectionStart;
         public int LeftSectionEnd;
         public int RightSectionStart;
         public int RightSectionEnd;
      }

      /// <summary>
      /// Note: filename1 or filename2 can be 'null'
      /// Throws ContextMakingException.
      /// </summary>
      public GitDiffAnalyzer(IGitRepository gitRepository,
         string sha1, string sha2, string filename1, string filename2)
      {
         _sections = getDiffSections(gitRepository, sha1, sha2, filename1, filename2);
      }

      public bool IsLineAddedOrModified(int linenumber)
      {
         foreach (GitDiffSection section in _sections)
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
         foreach (GitDiffSection section in _sections)
         {
            if (linenumber >= section.LeftSectionStart && linenumber < section.LeftSectionEnd)
            {
               return true;
            }
         }
         return false;
      }

      /// <summary>
      /// Throws ContextMakingException.
      /// </summary>
      static private IEnumerable<GitDiffSection> getDiffSections(IGitRepository gitRepository,
         string sha1, string sha2, string filename1, string filename2)
      {
         List<GitDiffSection> sections = new List<GitDiffSection>();

         GitDiffArguments arguments = new GitDiffArguments
         {
            Mode = GitDiffArguments.DiffMode.Context,
            CommonArgs = new GitDiffArguments.CommonArguments
            {
               Sha1 = sha1,
               Sha2 = sha2,
               Filename1 = filename1,
               Filename2 = filename2,
            },
            SpecialArgs = new GitDiffArguments.DiffContextArguments
            {
               Context = 0
            }
         };

         IEnumerable<string> diff = null;
         try
         {
            diff = gitRepository.Data?.Get(arguments);
         }
         catch (GitNotAvailableDataException ex)
         {
            throw new ContextMakingException("Cannot obtain git diff", ex);
         }

         if (diff == null)
         {
            throw new ContextMakingException("Cannot obtain git diff", null);
         }

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

      private readonly IEnumerable<GitDiffSection> _sections;
   }
}

