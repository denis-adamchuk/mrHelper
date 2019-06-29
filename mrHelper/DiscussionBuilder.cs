using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace mrHelper
{
   class DiscussionBuilder
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

      // What lines need to be included into Merge Request Discussion Position details
      public enum PositionState
      {
         OldLineOnly,
         NewLineOnly,
         Both,
         Undefined
      }

      public DiscussionBuilder(MergeRequestDetails mrDetails, DiffToolInfo difftoolInfo)
      {
         _mergeRequestDetails = mrDetails;
         _difftoolInfo = difftoolInfo;
         _positionState = getPositionState();
         if (_positionState == PositionState.Undefined)
         {
            Debug.Assert(false);
         }
      }

      public PositionState GetPositionState()
      {
         return _positionState;
      }

      public DiscussionParameters GetDiscussionParameters(string discussionBody, bool includeDiffToolContext)
      {
         DiscussionParameters parameters = new DiscussionParameters();
         parameters.Body = formatDiscussionBody(discussionBody, !includeDiffToolContext);
         parameters.Position = (includeDiffToolContext && _positionState != PositionState.Undefined)
            ? createPositionDetails(_positionState) : new Nullable<DiscussionParameters.PositionDetails>();
         return parameters;
      }

      private string getDiscussionHeader()
      {
         return "<b>" + _difftoolInfo.LeftSideFileNameBrief + "</b>"
              + " (line " + _difftoolInfo.LeftSideLineNumber.ToString() + ") <i>vs</i> "
              + "<b>" + _difftoolInfo.RightSideFileNameBrief + "</b>"
              + " (line " + _difftoolInfo.RightSideLineNumber.ToString() + ")";
      }

      private string formatDiscussionBody(string userDefinedBody, bool addHeader)
      {
         string header = addHeader ? (getDiscussionHeader() + "<br>") : "";
         string body = userDefinedBody;
         return header + body;
      }

      private DiscussionParameters.PositionDetails createPositionDetails(PositionState state)
      {
         DiscussionParameters.PositionDetails details = new DiscussionParameters.PositionDetails();
         details.BaseSHA = _mergeRequestDetails.BaseSHA;
         details.HeadSHA = _mergeRequestDetails.HeadSHA;
         details.StartSHA = _mergeRequestDetails.StartSHA;

         details.OldPath = _difftoolInfo.LeftSideFileNameBrief;
         details.NewPath = _difftoolInfo.RightSideFileNameBrief;

         switch (state)
         {
            case PositionState.NewLineOnly:
               details.OldLine = null;
               details.NewLine = _difftoolInfo.RightSideLineNumber.ToString();
               break;

            case PositionState.OldLineOnly:
               details.OldLine = _difftoolInfo.LeftSideLineNumber.ToString();
               details.NewLine = null;
               break;

            case PositionState.Both:
               details.OldLine = _difftoolInfo.LeftSideLineNumber.ToString();
               details.NewLine = _difftoolInfo.RightSideLineNumber.ToString();
               break;
         }

         return details;
      }

      PositionState getPositionState()
      {
         // Obtain git diff -U0 sections
         var sections = getDiffSections(_difftoolInfo.RightSideFileNameBrief);
        
         // First, check if we're at the right side
         if (!_difftoolInfo.IsLeftSideCurrent)
         {
            // If we are at the right side, check if a selected line was added/modified
            if (isAddedOrModified(sections, _difftoolInfo.RightSideLineNumber))
            {
               return PositionState.NewLineOnly;
            }
            // If selected line is not added/modified, we need to send a deleted line to Gitlab
            // Make sure that a line selected at the left side was deleted
            else if (isDeleted(sections, _difftoolInfo.LeftSideLineNumber))
            {
               return PositionState.OldLineOnly;
            }
         }
         else
         {
            // If we are the left side, let's check first if the selected line was deleted
            if (isDeleted(sections, _difftoolInfo.LeftSideLineNumber))
            {
               return PositionState.OldLineOnly;
            }
            // If selected line was not deleted, check a right-side line number
            // Make sure that it was added/modified
            else if (isAddedOrModified(sections, _difftoolInfo.RightSideLineNumber))
            {
               return PositionState.NewLineOnly;
            }
         }

         // If neither left nor right lines are neither deleted nor added/modified,
         // then the only acceptable way is that they are unchanged. Check if they are equal.
         // If they are not, fallback.
         if (checkIfLinesAreEqual(_difftoolInfo))
         {
            return PositionState.Both;
         }
         return PositionState.Undefined;
      }

      private bool checkIfLinesAreEqual(DiffToolInfo info)
      {
         string left = File.ReadLines(info.LeftSideFileNameFull).Skip(info.LeftSideLineNumber - 1).Take(1).First();
         string right = File.ReadLines(info.RightSideFileNameFull).Skip(info.RightSideLineNumber - 1).Take(1).First();
         return left == right;
      }

      private List<GitDiffSection> getDiffSections(string filename)
      {
         List<GitDiffSection> sections = new List<GitDiffSection>();

         List<string> diff = gitClient.Diff(_mergeRequestDetails.BaseSHA, _mergeRequestDetails.HeadSHA, filename);
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

      private bool isAddedOrModified(List<GitDiffSection> sections, int linenumber)
      {
         foreach (var section in sections)
         {
            if (linenumber >= section.RightSectionStart && linenumber < section.RightSectionEnd)
            {
               return true;
            }
         }
         return false;
      }

      private bool isDeleted(List<GitDiffSection> sections, int linenumber)
      {
         foreach (var section in sections)
         {
            if (linenumber >= section.LeftSectionStart && linenumber < section.LeftSectionEnd)
            {
               return true;
            }
         }
         return false;
      }

      private readonly PositionState _positionState;
      private readonly MergeRequestDetails _mergeRequestDetails;
      private readonly DiffToolInfo _difftoolInfo;
   }
}
