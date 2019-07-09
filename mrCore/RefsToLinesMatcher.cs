using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace mrCore
{
   // This 'matcher' matches SHA of two commits to the line/side information obtained from diff tool.
   // Result of match is 'Position' structure object which can be passed to Gitlab.
   //
   // Cost: one 'git diff -U0' for each Match() call and two 'git show' calls for each Match() call.
   public class RefsToLinesMatcher
   {
      // What lines need to be included into Merge Request Discussion Position
      private enum MatchResult
      {
         OldLineOnly,
         NewLineOnly,
         Both,
         Undefined
      }

      public RefsToLinesMatcher(GitRepository gitRepository)
      {
         _gitRepository = gitRepository;
      }

      public Position? Match(DiffRefs diffRefs, DiffToolInfo difftoolInfo)
      {
         MatchResult matchResult = match(diffRefs, difftoolInfo);
         return matchResult != MatchResult.Undefined
            ? createPosition(matchResult, diffRefs, difftoolInfo) : new Nullable<Position>();
      }

      private Position createPosition(MatchResult state, DiffRefs diffRefs, DiffToolInfo difftoolInfo)
      {
         Position position = new Position();
         position.Refs = diffRefs;

         position.OldPath = difftoolInfo.LeftSideFileNameBrief;
         position.NewPath = difftoolInfo.RightSideFileNameBrief;

         switch (state)
         {
            case MatchResult.NewLineOnly:
               position.OldLine = null;
               position.NewLine = difftoolInfo.RightSideLineNumber.ToString();
               break;

            case MatchResult.OldLineOnly:
               position.OldLine = difftoolInfo.LeftSideLineNumber.ToString();
               position.NewLine = null;
               break;

            case MatchResult.Both:
               position.OldLine = difftoolInfo.LeftSideLineNumber.ToString();
               position.NewLine = difftoolInfo.RightSideLineNumber.ToString();
               break;
         }

         return position;
      }

      MatchResult match(DiffRefs diffRefs, DiffToolInfo difftoolInfo)
      {
         // Obtain git diff -U0 sections
         GitDiffAnalyzer gitDiffAnalyzer = new GitDiffAnalyzer(_gitRepository,
            diffRefs.BaseSHA, diffRefs.HeadSHA, difftoolInfo.RightSideFileNameBrief);
        
         // First, check if we're at the right side
         if (!difftoolInfo.IsLeftSideCurrent)
         {
            // If we are at the right side, check if a selected line was added/modified
            if (gitDiffAnalyzer.IsLineAddedOrModified(difftoolInfo.RightSideLineNumber))
            {
               return MatchResult.NewLineOnly;
            }
            // If selected line is not added/modified, we need to send a deleted line to Gitlab
            // Make sure that a line selected at the left side was deleted
            else if (gitDiffAnalyzer.IsLineDeleted(difftoolInfo.LeftSideLineNumber))
            {
               return MatchResult.OldLineOnly;
            }
         }
         else
         {
            // If we are the left side, let's check first if the selected line was deleted
            if (gitDiffAnalyzer.IsLineDeleted(difftoolInfo.LeftSideLineNumber))
            {
               return MatchResult.OldLineOnly;
            }
            // If selected line was not deleted, check a right-side line number
            // Make sure that it was added/modified
            else if (gitDiffAnalyzer.IsLineAddedOrModified(difftoolInfo.RightSideLineNumber))
            {
               return MatchResult.NewLineOnly;
            }
         }

         // If neither left nor right lines are neither deleted nor added/modified,
         // then the only acceptable way is that they are unchanged. Check if they are equal.
         // If they are not, fallback.
         if (checkIfLinesAreEqual(diffRefs, difftoolInfo))
         {
            return MatchResult.Both;
         }
         return MatchResult.Undefined;
      }

      private bool checkIfLinesAreEqual(DiffRefs diffRefs, DiffToolInfo info)
      {
         List<string> left = _gitRepository.ShowFileByRevision(info.LeftSideFileNameBrief, diffRefs.BaseSHA);
         List<string> right = _gitRepository.ShowFileByRevision(info.RightSideFileNameBrief, diffRefs.HeadSHA);
         if (info.LeftSideLineNumber >= left.Count && info.RightSideLineNumber >= right.Count)
         {
            Debug.Assert(false);
            return false;
         }

         return left[info.LeftSideLineNumber] == right[info.RightSideLineNumber];
      }

      GitRepository _gitRepository;
   }
}
