using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using mrHelper.Core.Git;
using mrHelper.Core.Interprocess;
using mrHelper.Common.Interfaces;

namespace mrHelper.Core.Matching
{
   public class MatchException : Exception
   {
      public MatchException(DiffRefs diffRefs, DiffToolInfo diffToolInfo)
         : base(String.Format("Cannot match commits to diff tool information.\nDiffRefs: {0}\nDiffToolInfo: {1}",
            diffRefs.ToString(), diffToolInfo.ToString()))
      {
      }
   }

   /// <summary>
   /// Matches SHA of two commits to the line/side details obtained from diff tool.
   /// See matching rules in comments for DiffPosition structure.
   /// Cost: one 'git diff -U0' for each Match() call and two 'git show' calls for each Match() call.
   /// </summary>
   public class RefToLineMatcher
   {
      // Internal matching state
      private enum MatchResult
      {
         LeftLineOnly,
         RightLineOnly,
         Both,
         Undefined
      }

      public RefToLineMatcher(IGitRepository gitRepository)
      {
         _gitRepository = gitRepository;
      }

      /// <summary>
      /// Returns DiffPosition if match succeeded and throws if match failed
      /// Throws ArgumentException in case of bad arguments.
      /// Throws MatchException when match failed.
      /// Throws GitOperationException in case of problems with git.
      /// </summary>
      public DiffPosition Match(DiffRefs diffRefs, DiffToolInfo difftoolInfo)
      {
         if (!difftoolInfo.IsValid())
         {
            throw new ArgumentException(
               String.Format("Bad diff tool info: {0}", difftoolInfo.ToString()));
         }

         MatchResult matchResult = match(diffRefs, difftoolInfo);
         if (matchResult == MatchResult.Undefined)
         {
            throw new MatchException(diffRefs, difftoolInfo);
         }

         return createPosition(matchResult, diffRefs, difftoolInfo);
      }

      private DiffPosition createPosition(MatchResult state, DiffRefs diffRefs, DiffToolInfo difftoolInfo)
      {
         Debug.Assert(difftoolInfo.Left.HasValue || difftoolInfo.Right.HasValue);

         string leftPath = String.Empty;
         string rightPath = String.Empty;
         getPositionPaths(ref difftoolInfo, ref leftPath, ref rightPath);

         string leftLine = String.Empty;
         string rightLine = String.Empty;
         getPositionLines(state, ref difftoolInfo, ref leftLine, ref rightLine);

         DiffPosition position = new DiffPosition
         {
            Refs = diffRefs,
            LeftPath = leftPath,
            LeftLine = leftLine,
            RightPath = rightPath,
            RightLine = rightLine
         };
         return position;
      }

      private void getPositionPaths(ref DiffToolInfo difftoolInfo, ref string LeftPath, ref string RightPath)
      {
         if (difftoolInfo.Left.HasValue && !difftoolInfo.Right.HasValue)
         {
            // When a file is removed, diff tool does not provide a right-side file name
            LeftPath = difftoolInfo.Left.Value.FileName;
            RightPath = LeftPath;
         }
         else if (!difftoolInfo.Left.HasValue && difftoolInfo.Right.HasValue)
         {
            // When a file is added, diff tool does not provide a left-side file name
            LeftPath = difftoolInfo.Right.Value.FileName;
            RightPath = LeftPath;
         }
         else if (difftoolInfo.Left.HasValue && difftoolInfo.Right.HasValue)
         {
            // Filenames may be different and may be the same, it does not matter here
            LeftPath = difftoolInfo.Left.Value.FileName;
            RightPath = difftoolInfo.Right.Value.FileName;
         }
         else
         {
            Debug.Assert(false);
         }
      }

      private void getPositionLines(MatchResult state, ref DiffToolInfo difftoolInfo,
         ref string leftLine, ref string rightLine)
      {
         switch (state)
         {
            case MatchResult.RightLineOnly:
               Debug.Assert(difftoolInfo.Right.HasValue);

               leftLine = null;
               rightLine = difftoolInfo.Right.Value.LineNumber.ToString();
               break;

            case MatchResult.LeftLineOnly:
               Debug.Assert(difftoolInfo.Left.HasValue);

               leftLine = difftoolInfo.Left.Value.LineNumber.ToString();
               rightLine = null;
               break;

            case MatchResult.Both:
               Debug.Assert(difftoolInfo.Right.HasValue);
               Debug.Assert(difftoolInfo.Left.HasValue);

               leftLine = difftoolInfo.Left.Value.LineNumber.ToString();
               rightLine = difftoolInfo.Right.Value.LineNumber.ToString();
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
            diffRefs.LeftSHA, diffRefs.RightSHA,
            difftoolInfo.Left?.FileName ?? null, difftoolInfo.Right?.FileName ?? null);

         MatchResult result;
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
            result = MatchResult.RightLineOnly;
         }
         // Bad line number, a line belongs to the right side but it is not added
         else if (!difftoolInfo.Left.HasValue)
         {
            result = MatchResult.Undefined;
         }
         // If selected line is not added/modified, check a left-side line number
         // Make sure that it was deleted
         else if (gitDiffAnalyzer.IsLineDeleted(difftoolInfo.Left.Value.LineNumber))
         {
            result = MatchResult.LeftLineOnly;
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
            result = MatchResult.LeftLineOnly;
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
            result = MatchResult.RightLineOnly;
         }

         return result != MatchResult.Undefined;
      }

      private bool checkIfLinesAreEqual(DiffRefs diffRefs, DiffToolInfo info)
      {
         Debug.Assert(info.Left.HasValue && info.Right.HasValue);

         List<string> left = _gitRepository.ShowFileByRevision(info.Left.Value.FileName, diffRefs.LeftSHA);
         List<string> right = _gitRepository.ShowFileByRevision(info.Right.Value.FileName, diffRefs.RightSHA);
         if (info.Left.Value.LineNumber > left.Count || info.Right.Value.LineNumber > right.Count)
         {
            Debug.Assert(false);
            return false;
         }

         return left[info.Left.Value.LineNumber - 1] == right[info.Right.Value.LineNumber - 1];
      }

      private readonly IGitRepository _gitRepository;
   }
}

