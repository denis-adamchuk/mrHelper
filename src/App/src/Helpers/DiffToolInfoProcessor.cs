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
      public DiffToolInfo Process(DiffToolInfo diffToolInfo, Core.Matching.DiffRefs refs)
      {
         string currentName;
         string anotherName;
         bool moved;
         bool renamed;
         try
         {
            renamed = checkForRenamedFile(refs, diffToolInfo, out currentName, out anotherName, out moved);
         }
         catch (GitOperationException)
         {
            throw; // fatal error
         }

         return renamed ? handleFileRename(diffToolInfo, currentName, anotherName, moved) : diffToolInfo;
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

      private DiffToolInfo handleFileRename(DiffToolInfo diffToolInfo,
         string currentName, string anotherName, bool moved)
      {
         if (moved)
         {
            Trace.TraceInformation("Detected file move. DiffToolInfo: {0}", diffToolInfo);

            MessageBox.Show(
              "Merge Request Helper detected that current file is a moved version of another file. "
            + "GitLab does not allow to create discussions on moved files.\n\n"
            + "Current file:\n"
            + currentName + "\n\n"
            + "Another file:\n"
            + anotherName,
              "Cannot create a discussion",
              MessageBoxButtons.OK, MessageBoxIcon.Warning);

            throw new DiffToolInfoProcessorException();
         }

         Trace.TraceInformation("Detected file rename. DiffToolInfo: {0}", diffToolInfo);

         bool isLeftSide = diffToolInfo.IsLeftSideCurrent;
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
               MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.No)
         {
            Trace.TraceInformation("User decided to match files manually");
            throw new DiffToolInfoProcessorException();
         }

         DiffToolInfo result = createDiffToolInfoCloneWithFakeSide(diffToolInfo, anotherName);
         Trace.TraceInformation("Updated DiffToolInfo: {0}", result);
         return result;
      }

      DiffToolInfo createDiffToolInfoCloneWithFakeSide(DiffToolInfo source, string anotherName)
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

