using System;
using System.Diagnostics;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;
using mrHelper.Core.Git;
using mrHelper.Core.Matching;
using mrHelper.Core.Interprocess;
using System.Windows.Forms;

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
      public bool Process(DiffToolInfo source, Core.Matching.DiffRefs refs, out LineMatchInfo dest)
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

         dest = createLineMatchInfo(source, anotherName);
         if (!renamed)
         {
            return true;
         }

         Trace.TraceInformation(String.Format(
            "Detected file {0}. Git repository path: {1}. DiffRefs: {2}\nDiffToolInfo: {3}\nLineMatchInfo: {4}",
                  (moved ? "move" : "rename"), _gitRepository.Path, refs.ToString(), source.ToString(), dest.ToString()));

         return handleFileRename(source.IsLeftSide, currentName, anotherName, moved);
      }

      /// <summary>
      /// Throws GitOperationException and GitObjectException in case of problems with git.
      /// </summary>
      private bool checkForRenamedFile(Core.Matching.DiffRefs refs, DiffToolInfo diffToolInfo,
         out string currentName, out string anotherName, out bool moved)
      {
         GitRenameDetector renameChecker = new GitRenameDetector(_gitRepository);
         if (!diffToolInfo.IsLeftSide)
         {
            currentName = diffToolInfo.FileName;
            anotherName = renameChecker.IsRenamed(
               refs.LeftSHA,
               refs.RightSHA,
               diffToolInfo.FileName,
               false, out moved);
         }
         else
         {
            currentName = diffToolInfo.FileName;
            anotherName = renameChecker.IsRenamed(
               refs.LeftSHA,
               refs.RightSHA,
               diffToolInfo.FileName,
               true, out moved);
         }

         if (moved || anotherName != currentName)
         {
            return true;
         }

         // it is not a renamed but a new/removed file
         anotherName = String.Empty;
         return false;
      }

      private static bool handleFileRename(bool isLeftSideCurrent, string currentName, string anotherName, bool moved)
      {
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

         string fileStatus = isLeftSideCurrent ? "new" : "deleted";

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

         return true;
      }

      private static LineMatchInfo createLineMatchInfo(DiffToolInfo source, string anotherName)
      {
         Debug.Assert(source.LineNumber > 0);
         Debug.Assert(anotherName != String.Empty);

         return new LineMatchInfo
         {
            IsLeftSideLineNumber = source.IsLeftSide,
            LeftFileName = source.IsLeftSide ? source.FileName : anotherName,
            RightFileName = source.IsLeftSide ? anotherName : source.FileName,
            LineNumber = source.LineNumber
         };
      }

      private readonly IGitRepository _gitRepository;
   }
}

