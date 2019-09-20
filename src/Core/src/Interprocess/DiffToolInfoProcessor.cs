using System;
using System.Diagnostics;
using mrHelper.Common.Interfaces;
using mrHelper.Core.Git;
using mrHelper.Core.Matching;
using mrHelper.Core.Interprocess;

namespace mrHelper.Core.Interprocess
{
   /// <summary>
   /// Converts DiffToolInfo into LineMatchInfo
   /// </summary>
   public class DiffToolInfoProcessor
   {
      public DiffToolInfoProcessor(IGitRepository gitRepository,
         Func<string, string, bool> onFileMove, Func<string, string, bool, bool> onFileRename)
      {
         _gitRepository = gitRepository;
         _onFileMove = onFileMove;
         _onFileRename = onFileRename;
      }

      /// <summary>
      /// Throws GitOperationException in case of problems with git.
      /// </summary>
      public bool Process(DiffToolInfo source, Core.Matching.DiffRefs refs, out LineMatchInfo dest)
      {
         GitRenameDetector renameChecker = new GitRenameDetector(_gitRepository);

         string anotherName = renameChecker.IsRenamed(refs.LeftSHA, refs.RightSHA,
            source.FileName, source.IsLeftSide, out bool moved);
         bool renamed = moved || anotherName != source.FileName;

         dest = createLineMatchInfo(source, renamed ? anotherName : String.Empty);
         if (!renamed)
         {
            return true;
         }

         Trace.TraceInformation(String.Format(
            "Detected file {0}. Git repository path: {1}. DiffRefs: {2}\nDiffToolInfo: {3}\nLineMatchInfo: {4}",
                  (moved ? "move" : "rename"), _gitRepository.Path, refs.ToString(), source.ToString(), dest.ToString()));

         return moved ? _onFileMove(source.FileName, anotherName)
                      : _onFileRename(source.FileName, anotherName, source.IsLeftSide);
      }

      private static LineMatchInfo createLineMatchInfo(DiffToolInfo source, string anotherName)
      {
         Debug.Assert(source.LineNumber > 0);

         return new LineMatchInfo
         {
            IsLeftSideLineNumber = source.IsLeftSide,
            LeftFileName = source.IsLeftSide ? source.FileName : anotherName,
            RightFileName = source.IsLeftSide ? anotherName : source.FileName,
            LineNumber = source.LineNumber
         };
      }

      private readonly IGitRepository _gitRepository;
      private readonly Func<string, string, bool> _onFileMove;
      private readonly Func<string, string, bool, bool> _onFileRename;
   }
}

