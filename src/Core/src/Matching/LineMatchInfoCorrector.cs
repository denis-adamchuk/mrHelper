using System;
using System.Diagnostics;
using mrHelper.Common.Interfaces;
using mrHelper.Core.Git;

namespace mrHelper.Core.Matching
{
   /// <summary>
   /// Converts LineMatchInfo into LineMatchInfo
   /// </summary>
   public class LineMatchInfoCorrector
   {
      public LineMatchInfoCorrector(IGitRepository gitRepository,
         Action<string, string> onFileMove, Func<string, string, string, bool> onFileRename, Action onWrongMatch)
      {
         _gitRepository = gitRepository;
         _onFileMove = onFileMove;
         _onFileRename = onFileRename;
         _onWrongMatch = onWrongMatch;
      }

      /// <summary>
      /// Throws GitOperationException in case of problems with git.
      /// </summary>
      public LineMatchInfo? Correct(LineMatchInfo source, Core.Matching.DiffRefs refs)
      {
         bool isLeftSide = source.IsLeftSideLineNumber;
         string currentName = isLeftSide ? source.LeftFileName : source.RightFileName;
         string oppositeName = isLeftSide ? source.RightFileName : source.LeftFileName;
         if (!getOppositeName(refs, isLeftSide, currentName, oppositeName, out oppositeName))
         {
            return new Nullable<LineMatchInfo>();
         }

         return new LineMatchInfo
            {
               IsLeftSideLineNumber = isLeftSide,
               LeftFileName = isLeftSide ? currentName : oppositeName,
               RightFileName = isLeftSide ? oppositeName : currentName,
               LineNumber = source.LineNumber
            };
      }

      private bool getOppositeName(Core.Matching.DiffRefs refs, bool isLeftSide,
         string sourceCurrentName, string sourceOppositeName, out string destOppositeName)
      {
         Debug.Assert(sourceCurrentName != String.Empty);
         destOppositeName = String.Empty;

         GitRenameDetector renameChecker = new GitRenameDetector(_gitRepository);
         string anotherName = renameChecker.IsRenamed(refs.LeftSHA, refs.RightSHA,
            sourceCurrentName, isLeftSide, out bool moved);
         if (moved)
         {
            _onFileMove(sourceCurrentName, anotherName);
            trace("move", refs, sourceCurrentName, sourceOppositeName, String.Empty);
            return false;
         }

         Debug.Assert(anotherName != String.Empty);
         if (sourceOppositeName == String.Empty)
         {
            // nothing at the opposite side
            if (anotherName != sourceCurrentName)
            {
               // wrong match, propose to re-match or consider new/deleted
               if (_onFileRename(sourceCurrentName, anotherName, isLeftSide ? "deleted" : "new"))
               {
                  // discard rename. correct destOppositeName to avoid GitLab errors.
                  destOppositeName = anotherName;
                  trace("rename (discard)", refs, sourceCurrentName, sourceOppositeName, destOppositeName);
                  return true;
               }
               else
               {
                  // user will re-match
                  trace("rename (re-match)", refs, sourceCurrentName, sourceOppositeName, String.Empty);
                  return false;
               }
            }
            else
            {
               // TODO This is a weak place. If file was manually matched with emptiness, GitLab will fail.
               // Need to check for added/deleted file names via git.
               destOppositeName = String.Empty;
               return true;
            }
         }
         else
         {
            // opposite side is not empty
            if (anotherName != sourceCurrentName)
            {
               // rename detected
               if (anotherName == sourceOppositeName)
               {
                  // manually matched files, perfect match
                  destOppositeName = sourceOppositeName;
                  return true;
               }
               else
               {
                  // wrong match, propose to re-match
                  if (_onFileRename(sourceCurrentName, anotherName, "modified"))
                  {
                     // discard rename. correct destOppositeName to avoid GitLab errors.
                     destOppositeName = anotherName;
                     trace("rename (discard)", refs, sourceCurrentName, sourceOppositeName, destOppositeName);
                     return true;
                  }
                  else
                  {
                     // user will re-match
                     trace("rename (re-match)", refs, sourceCurrentName, sourceOppositeName, String.Empty);
                     return false;
                  }
               }
            }
            else
            {
               if (sourceCurrentName != sourceOppositeName)
               {
                  _onWrongMatch();
                  trace("wrong match", refs, sourceCurrentName, sourceOppositeName, String.Empty);
                  return false;
               }

               // it is not a rename but really modified file detected
               destOppositeName = sourceOppositeName;
               return true;
            }
         }
      }

      private void trace(string action, DiffRefs refs, string sourceCurrentName, string sourceOppositeName,
         string destOppositeName)
      {
         Trace.TraceInformation(String.Format(
            "[LineMatchInfoCorrector] {0}. Git repository path: {1}. DiffRefs: {2}\n"
          + "sourceCurrentName: {3}\nsourceOppositeName: {4}\ndestOppositeName: {5}",
               action, _gitRepository.Path, refs.ToString(), sourceCurrentName, sourceOppositeName, destOppositeName));
      }

      private readonly IGitRepository _gitRepository;

      /// <summary>
      /// Notify user about impossible match
      /// </summary>
      private readonly Action _onWrongMatch;

      /// <summary>
      /// Notify user about file move
      /// </summary>
      private readonly Action<string, string> _onFileMove;

      /// <summary>
      /// Notify user about file rename. Return true if rename should be ignored.
      /// </summary>
      private readonly Func<string, string, string, bool> _onFileRename;
   }
}

