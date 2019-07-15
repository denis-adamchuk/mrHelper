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
         Debug.Assert(difftoolInfo.Left.HasValue || difftoolInfo.Right.HasValue);

         string oldPath = String.Empty;
         string newPath = String.Empty;
         getPositionPaths(ref difftoolInfo, ref oldPath, ref newPath);

         string oldLine = String.Empty;
         string newLine = String.Empty;
         getPositionLines(state, ref difftoolInfo, ref oldLine, ref newLine);

         Position position = new Position
         {
            Refs = diffRefs,
            OldPath = oldPath,
            OldLine = oldLine,
            NewPath = newPath,
            NewLine = newLine
         };
         return position;
      }

      private void getPositionPaths(ref DiffToolInfo difftoolInfo, ref string oldPath, ref string newPath)
      {
         if (difftoolInfo.Left.HasValue && !difftoolInfo.Right.HasValue)
         {
            // When a file is removed, diff tool does not provide a right-side file name, but GitLab needs both
            oldPath = difftoolInfo.Left.Value.FileName;
            newPath = oldPath;
         }
         else if (!difftoolInfo.Left.HasValue && difftoolInfo.Right.HasValue)
         {
            // When a file is added, diff tool does not provide a left-side file name, but GitLab needs both
            oldPath = difftoolInfo.Right.Value.FileName;
            newPath = oldPath;
         }
         else if (difftoolInfo.Left.HasValue && difftoolInfo.Right.HasValue)
         {
            // Filenames may be different and may be the same, it does not matter here, provide them both to GitLab
            oldPath = difftoolInfo.Left.Value.FileName;
            newPath = difftoolInfo.Right.Value.FileName;
         }
         else
         {
            Debug.Assert(false);
         }
      }

      private void getPositionLines(MatchResult state, ref DiffToolInfo difftoolInfo,
         ref string oldLine, ref string newLine)
      {
         switch (state)
         {
            case MatchResult.NewLineOnly:
               Debug.Assert(difftoolInfo.Right.HasValue);

               oldLine = null;
               newLine = difftoolInfo.Right.Value.LineNumber.ToString();
               break;

            case MatchResult.OldLineOnly:
               Debug.Assert(difftoolInfo.Left.HasValue);

               oldLine = difftoolInfo.Left.Value.LineNumber.ToString();
               newLine = null;
               break;

            case MatchResult.Both:
               Debug.Assert(difftoolInfo.Right.HasValue);
               Debug.Assert(difftoolInfo.Left.HasValue);

               oldLine = difftoolInfo.Left.Value.LineNumber.ToString();
               newLine = difftoolInfo.Right.Value.LineNumber.ToString();
               break;

            case MatchResult.Undefined:
               Debug.Assert(false);
               break;
         }
      }

      private MatchResult match(DiffRefs diffRefs, DiffToolInfo difftoolInfo)
      {
         // Obtain git diff -U0 sections
         GitDiffAnalyzer gitDiffAnalyzer = new GitDiffAnalyzer(_gitRepository,
            diffRefs.BaseSHA, diffRefs.HeadSHA,
            difftoolInfo.Left?.FileName ?? null, difftoolInfo.Right?.FileName ?? null);

         MatchResult result = MatchResult.Undefined;
         if (!difftoolInfo.IsLeftSideCurrent && matchRightSide(difftoolInfo, gitDiffAnalyzer, out result))
         {
            return result;
         }
         else if (difftoolInfo.IsLeftSideCurrent && matchLeftSide(difftoolInfo, gitDiffAnalyzer, out result))
         {
            return result;
         }
         else if (checkIfLinesAreEqual(diffRefs, difftoolInfo))
         {
            // If neither left nor right lines are neither deleted nor added/modified,
            // then the only acceptable way is that they are unchanged. Check if they are equal.
            return MatchResult.Both;
         }

         // Difftool info is invalid
         return MatchResult.Undefined;
      }

      bool matchRightSide(DiffToolInfo difftoolInfo, GitDiffAnalyzer gitDiffAnalyzer, out MatchResult result)
      {
         Debug.Assert(difftoolInfo.Right.HasValue);

         result = MatchResult.Undefined;

         // If we are at the right side, check if a selected line was added/modified
         if (gitDiffAnalyzer.IsLineAddedOrModified(difftoolInfo.Right.Value.LineNumber))
         {
            result = MatchResult.NewLineOnly;
         }
         // Bad line number, a line belongs to the right side but it is not added
         else if (!difftoolInfo.Left.HasValue)
         {
            result = MatchResult.Undefined;
         }
         // If selected line is not added/modified, we need to send a deleted line to Gitlab
         // Make sure that a line selected at the left side was deleted
         else if (gitDiffAnalyzer.IsLineDeleted(difftoolInfo.Left.Value.LineNumber))
         {
            result = MatchResult.OldLineOnly;
         }

         return result != MatchResult.Undefined;
      }

      bool matchLeftSide(DiffToolInfo difftoolInfo, GitDiffAnalyzer gitDiffAnalyzer, out MatchResult result)
      {
         Debug.Assert(difftoolInfo.Left.HasValue);

         result = MatchResult.Undefined;

         // If we are the left side, let's check first if the selected line was deleted
         if (gitDiffAnalyzer.IsLineDeleted(difftoolInfo.Left.Value.LineNumber))
         {
            result = MatchResult.OldLineOnly;
         }
         // Bad line number, a line belongs to the left side but it is not removed
         else if (!difftoolInfo.Right.HasValue)
         {
            result = MatchResult.Undefined;
         }
         // If selected line was not deleted, check a right-side line number
         // Make sure that it was added/modified
         else if (gitDiffAnalyzer.IsLineAddedOrModified(difftoolInfo.Right.Value.LineNumber))
         {
            result = MatchResult.NewLineOnly;
         }

         return result != MatchResult.Undefined;
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

      private readonly GitRepository _gitRepository;
   }
}

