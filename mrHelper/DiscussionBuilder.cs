using System;
using System.Collections.Generic;
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

      // What lines need to be included into Merge Request Discussion Position details
      enum PositionState
      {
         OldLineOnly,
         NewLineOnly,
         Both,
      }

      public DiscussionBuilder(MergeRequestDetails mrDetails, DiffToolInfo difftoolInfo)
      {
         _mergeRequestDetails = mrDetails;
         _difftoolInfo = difftoolInfo;
      }

      public DiscussionParameters GetDiscussionParameters(string discussionBody, bool includeDiffToolContext)
      {
         DiscussionParameters parameters = new DiscussionParameters();
         parameters.Body = formatDiscussionBody(discussionBody, !includeDiffToolContext);
         parameters.Position = includeDiffToolContext
            ? createPositionDetails(getPositionState()) : new Nullable<DiscussionParameters.PositionDetails>();
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
         // TODO This should not affect code snippets (fragments with ```)
         //string body = userDefinedBody.Replace("\r\n", "<br>").Replace("\n", "<br>"); 
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
         if (checkIfLinesAreEqual(_difftoolInfo))
         {
            return PositionState.Both;
         }

         if (_difftoolInfo.IsLeftSideCurrent)
         {
            return PositionState.OldLineOnly;
         }

         return PositionState.NewLineOnly;
      }

      private bool checkIfLinesAreEqual(DiffToolInfo info)
      {
         string left = File.ReadLines(info.LeftSideFileNameFull).Skip(info.LeftSideLineNumber - 1).Take(1).First();
         string right = File.ReadLines(info.RightSideFileNameFull).Skip(info.RightSideLineNumber - 1).Take(1).First();
         return left == right;
      }

      private readonly MergeRequestDetails _mergeRequestDetails;
      private readonly DiffToolInfo _difftoolInfo;
   }
}
