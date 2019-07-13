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

      // Returns Position if match succeeded and null if match failed, what most likely means that
      // diff tool info is invalid
      public Position? Match(DiffRefs diffRefs, DiffToolInfo difftoolInfo)
      {
         if (!difftoolInfo.IsValid())
         {
            Debug.Assert(false);
            return new Nullable<Position>();
         }

         MatchResult matchResult = match(diffRefs, difftoolInfo);
         return matchResult != MatchResult.Undefined
            ? createPosition(matchResult, diffRefs, difftoolInfo) : new Nullable<Position>();
      }

      private Position createPosition(MatchResult state, DiffRefs diffRefs, DiffToolInfo difftoolInfo)
      {
         Position position = new Position();
         position.Refs = diffRefs;

         // GitLab expects two filenames in all cases and the only case when they are different is file rename.
         position.OldPath = difftoolInfo.Left?.FileName ?? difftoolInfo.Right.Value.FileName;
         position.NewPath = difftoolInfo.Right?.FileName ?? difftoolInfo.Left.Value.FileName;

         switch (state)
         {
            case MatchResult.NewLineOnly:
               Debug.Assert(difftoolInfo.Right.HasValue);

               position.OldLine = null;
               position.NewLine = difftoolInfo.Right.Value.LineNumber.ToString();
               break;

            case MatchResult.OldLineOnly:
               Debug.Assert(difftoolInfo.Left.HasValue);

               position.OldLine = difftoolInfo.Left.Value.LineNumber.ToString();
               position.NewLine = null;
               break;

            case MatchResult.Both:
               Debug.Assert(difftoolInfo.Right.HasValue);
               Debug.Assert(difftoolInfo.Left.HasValue);

               position.OldLine = difftoolInfo.Left.Value.LineNumber.ToString();
               position.NewLine = difftoolInfo.Right.Value.LineNumber.ToString();
               break;

            case MatchResult.Undefined:
               Debug.Assert(false);
               break;
         }

         return position;
      }

      MatchResult match(DiffRefs diffRefs, DiffToolInfo difftoolInfo)
      {
         // Obtain git diff -U0 sections
         GitDiffAnalyzer gitDiffAnalyzer = new GitDiffAnalyzer(_gitRepository,
            diffRefs.BaseSHA, diffRefs.HeadSHA,
            difftoolInfo.Left?.FileName ?? null, difftoolInfo.Right?.FileName ?? null);
        
         // First, check if we're at the right side
         if (!difftoolInfo.IsLeftSideCurrent)
         {
            Debug.Assert(difftoolInfo.Right.HasValue);

            // If we are at the right side, check if a selected line was added/modified
            if (gitDiffAnalyzer.IsLineAddedOrModified(difftoolInfo.Right.Value.LineNumber))
            {
               return MatchResult.NewLineOnly;
            }
            // If selected line is not added/modified, we need to send a deleted line to Gitlab
            // Make sure that a line selected at the left side was deleted
            else if (difftoolInfo.Left.HasValue
               && gitDiffAnalyzer.IsLineDeleted(difftoolInfo.Left.Value.LineNumber))
            {
               return MatchResult.OldLineOnly;
            }
         }
         else
         {
            Debug.Assert(difftoolInfo.Left.HasValue);

            // If we are the left side, let's check first if the selected line was deleted
            if (gitDiffAnalyzer.IsLineDeleted(difftoolInfo.Left.Value.LineNumber))
            {
               return MatchResult.OldLineOnly;
            }
            // If selected line was not deleted, check a right-side line number
            // Make sure that it was added/modified
            else if (difftoolInfo.Right.HasValue
               && gitDiffAnalyzer.IsLineAddedOrModified(difftoolInfo.Right.Value.LineNumber))
            {
               return MatchResult.NewLineOnly;
            }
         }

         if (!difftoolInfo.Left.HasValue || !difftoolInfo.Right.HasValue)
         {
            // If we are here, difftool info is invalid and cannot be matched with SHAs
            Debug.Assert(false);
            return MatchResult.Undefined;
         }

         // If neither left nor right lines are neither deleted nor added/modified,
         // then the only acceptable way is that they are unchanged. Check if they are equal.
         // If they are not, fallback.
         if (checkIfLinesAreEqual(diffRefs, difftoolInfo))
         {
            return MatchResult.Both;
         }

         // Difftool info is invalid
         return MatchResult.Undefined;
      }

      private bool checkIfLinesAreEqual(DiffRefs diffRefs, DiffToolInfo info)
      {
         Debug.Assert(info.Left.HasValue && info.Right.HasValue);

         List<string> left = _gitRepository.ShowFileByRevision(info.Left.Value.FileName, diffRefs.BaseSHA);
         List<string> right = _gitRepository.ShowFileByRevision(info.Right.Value.FileName, diffRefs.HeadSHA);
         if (info.Left.Value.LineNumber > left.Count && info.Right.Value.LineNumber> right.Count)
         {
            Debug.Assert(false);
            return false;
         }

         return left[info.Left.Value.LineNumber - 1] == right[info.Right.Value.LineNumber - 1];
      }

      GitRepository _gitRepository;
   }
}

