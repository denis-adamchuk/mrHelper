using System;
using System.Diagnostics;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using mrHelper.Core.Interprocess;
using mrHelper.Core.Git;
using System.Windows.Forms;
using mrHelper.Common.Exceptions;

namespace mrHelper.Forms.Helpers
{
   public class DiffToolInfoProcessorException : Exception {}

   /// <summary>
   /// Processes DiffToolInfo before it can be used to create DiffPosition
   /// </summary>
   public class DiffToolInfoProcessor
   {
      public DiffToolInfoProcessor(IGitRepository gitRepository)
      {
         _gitRepository = gitRepository;
      }

      /// <summary>
      /// Throws GitOperationException in case of problems with git.
      /// </summary>
      public bool Process(DiffToolInfo source, Core.Matching.DiffRefs refs, out DiffToolInfo dest)
      {
         string currentName;
         string anotherName;
         bool moved;
         bool renamed;
         try
         {
            renamed = checkForRenamedFile(refs, source, out currentName, out anotherName, out moved);
         }
         catch (GitOperationException)
         {
            throw; // fatal error
         }

         if (renamed)
         {
            Trace.TraceInformation("Detected file {0}. Git repository path: {1}. DiffRefs: {2}\nDiffToolInfo: {3}",
               (moved ? "move" : "rename"), _gitRepository.Path, refs.ToString(), source.ToString());
         }

         dest = source;
         return !renamed || handleFileRename(source, currentName, anotherName, moved, out dest);
      }

      /// <summary>
      /// Throws GitOperationException and GitObjectException in case of problems with git.
      /// </summary>
      private bool checkForRenamedFile(Core.Matching.DiffRefs refs, DiffToolInfo diffToolInfo,
         out string currentName, out string anotherName, out bool moved)
      {
         GitRenameDetector renameChecker = new GitRenameDetector(_gitRepository);
         if (!diffToolInfo.Left.HasValue)
         {
            Debug.Assert(diffToolInfo.Right.HasValue);
            currentName = diffToolInfo.Right?.FileName;
            anotherName = renameChecker.IsRenamed(
               refs.LeftSHA,
               refs.RightSHA,
               diffToolInfo.Right?.FileName,
               false, out moved);
            if (anotherName == diffToolInfo.Right?.FileName)
            {
               // it is not a renamed but removed file
               return false;
            }
         }
         else if (!diffToolInfo.Right.HasValue)
         {
            Debug.Assert(diffToolInfo.Left.HasValue);
            currentName = diffToolInfo.Left?.FileName;
            anotherName = renameChecker.IsRenamed(
               refs.LeftSHA,
               refs.RightSHA,
               diffToolInfo.Left?.FileName,
               true, out moved);
            if (anotherName == diffToolInfo.Left?.FileName)
            {
               // it is not a renamed but added file
               return false;
            }
         }
         else
         {
            // If even two names are given, we need to check here because use might selected manually two
            // versions of a moved file
            bool isLeftSide = diffToolInfo.IsLeftSideCurrent;
            currentName = isLeftSide ? diffToolInfo.Left?.FileName : diffToolInfo.Right?.FileName;
            anotherName = renameChecker.IsRenamed(
               refs.LeftSHA,
               refs.RightSHA,
               currentName,
               isLeftSide, out moved);
            return moved;
         }
         return true;
      }

      private static bool handleFileRename(DiffToolInfo source,
         string currentName, string anotherName, bool moved, out DiffToolInfo dest)
      {
         dest = source;

         if (moved)
         {
            MessageBox.Show(
              "Merge Request Helper detected that current file is a moved version of another file. "
            + "GitLab does not allow to create discussions on moved files.\n\n"
            + "Current file:\n"
            + currentName + "\n\n"
            + "Another file:\n"
            + anotherName,
              "Cannot create a discussion",
              MessageBoxButtons.OK, MessageBoxIcon.Warning);

            return false;
         }

         bool isLeftSide = source.IsLeftSideCurrent;
         string fileStatus = isLeftSide ? "new" : "deleted";

         if (MessageBox.Show(
               "Merge Request Helper detected that current file is a renamed version of another file. "
             + "Do you really want to review this file as a " + fileStatus + " file? "
             + "It is recommended to press \"No\" and match files manually in the diff tool.\n"
             + "Current file:\n"
             + currentName + "\n\n"
             + "Another file:\n"
             + anotherName,
               "Cannot create a discussion",
               MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2) == DialogResult.No)
         {
            Trace.TraceInformation("User decided to match files manually");
            return false;
         }

         dest = createDiffToolInfoCloneWithFakeSide(source, anotherName);
         Trace.TraceInformation("Updated DiffToolInfo: {0}", dest.ToString());
         return true;
      }

      private static DiffToolInfo createDiffToolInfoCloneWithFakeSide(DiffToolInfo source, string anotherName)
      {
         bool isLeftSide = source.IsLeftSideCurrent;
         return new DiffToolInfo
         {
            IsLeftSideCurrent = isLeftSide,
            Left = new DiffToolInfo.Side
            {
               FileName = isLeftSide ? source.Left.Value.FileName : anotherName,
               LineNumber = isLeftSide ? source.Left.Value.LineNumber : DiffToolInfo.Side.UninitializedLineNumber
            },
            Right = new DiffToolInfo.Side
            {
               FileName = isLeftSide ? anotherName : source.Right.Value.FileName,
               LineNumber = isLeftSide ? DiffToolInfo.Side.UninitializedLineNumber : source.Right.Value.LineNumber
            },
         };
      }

      private readonly IGitRepository _gitRepository;
   }
}

