using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace mrCore
{
   // TODO - This should be PositionDetailsBuilder and not DiscussionBuilder, let's get rid of 'body' mess here
   public class DiscussionBuilder
   {
      // What lines need to be included into Merge Request Discussion Position details
      public enum PositionState
      {
         OldLineOnly,
         NewLineOnly,
         Both,
         Undefined
      }

      public DiscussionBuilder(DiffRefs diffRefs, DiffToolInfo difftoolInfo)
      {
         _diffRefs = diffRefs;
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
            ? createPositionDetails(_positionState) : new Nullable<PositionDetails>();
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

      private PositionDetails createPositionDetails(PositionState state)
      {
         PositionDetails details = new PositionDetails();
         details.Refs = _diffRefs;

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
         GitDiffAnalyzer gitDiffAnalyzer = new GitDiffAnalyzer(
            _diffRefs.BaseSHA, _diffRefs.HeadSHA, _difftoolInfo.RightSideFileNameBrief);
        
         // First, check if we're at the right side
         if (!_difftoolInfo.IsLeftSideCurrent)
         {
            // If we are at the right side, check if a selected line was added/modified
            if (gitDiffAnalyzer.IsLineAddedOrModified(_difftoolInfo.RightSideLineNumber))
            {
               return PositionState.NewLineOnly;
            }
            // If selected line is not added/modified, we need to send a deleted line to Gitlab
            // Make sure that a line selected at the left side was deleted
            else if (gitDiffAnalyzer.IsLineDeleted(_difftoolInfo.LeftSideLineNumber))
            {
               return PositionState.OldLineOnly;
            }
         }
         else
         {
            // If we are the left side, let's check first if the selected line was deleted
            if (gitDiffAnalyzer.IsLineDeleted(_difftoolInfo.LeftSideLineNumber))
            {
               return PositionState.OldLineOnly;
            }
            // If selected line was not deleted, check a right-side line number
            // Make sure that it was added/modified
            else if (gitDiffAnalyzer.IsLineAddedOrModified(_difftoolInfo.RightSideLineNumber))
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

      private readonly PositionState _positionState;
      private readonly DiffRefs _diffRefs;
      private readonly DiffToolInfo _difftoolInfo;
   }
}
