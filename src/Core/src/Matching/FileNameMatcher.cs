using System;
using System.Diagnostics;
using mrHelper.Common.Interfaces;
using mrHelper.Core.Git;

namespace mrHelper.Core.Matching
{
   /// <summary>
   /// Fills Paths of DiffPosition
   /// </summary>
   public class FileNameMatcher
   {
      public FileNameMatcher(IGitRepository gitRepository,
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
      public bool Match(MatchInfo matchInfo, DiffPosition inDiffPosition, out DiffPosition outDiffPosition)
      {
         if (!matchInfo.IsValid())
         {
            throw new ArgumentException(
               String.Format("Bad match info: {0}", matchInfo.ToString()));
         }

         DiffRefs refs = inDiffPosition.Refs;
         bool isLeftSide = matchInfo.IsLeftSideLineNumber;
         string currentName = isLeftSide ? matchInfo.LeftFileName : matchInfo.RightFileName;
         string oppositeName = isLeftSide ? matchInfo.RightFileName : matchInfo.LeftFileName;
         oppositeName = getOppositeName(refs, isLeftSide, currentName, oppositeName);

         outDiffPosition = inDiffPosition;
         if (oppositeName == null)
         {
            return false;
         }

         outDiffPosition.LeftPath = isLeftSide ? currentName : oppositeName;
         outDiffPosition.RightPath = isLeftSide ? oppositeName : currentName;
         return true;
      }

      private string getOppositeName(DiffRefs refs, bool isLeftSide, string sourceCurrentName, string sourceOppositeName)
      {
         Debug.Assert(sourceCurrentName != String.Empty);

         GitRenameDetector renameChecker = new GitRenameDetector(_gitRepository);
         string anotherName = renameChecker.IsRenamed(refs.LeftSHA, refs.RightSHA,
            sourceCurrentName, isLeftSide, out bool moved);
         if (moved)
         {
            _onFileMove(sourceCurrentName, anotherName);
            trace("move", refs, sourceCurrentName, sourceOppositeName, String.Empty);
            return null;
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
                  // discard rename. fix up the opposite name to deceive GitLab.
                  trace("rename (discard)", refs, sourceCurrentName, sourceOppositeName, anotherName);
                  return anotherName;
               }
               else
               {
                  // user will re-match
                  trace("rename (re-match)", refs, sourceCurrentName, sourceOppositeName, String.Empty);
                  return null;
               }
            }
            else
            {
               // GitLab expects a non-empty name in this case
               return sourceCurrentName;
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
                  return sourceOppositeName;
               }
               else
               {
                  // wrong match, propose to re-match
                  if (_onFileRename(sourceCurrentName, anotherName, "modified"))
                  {
                     // discard rename. fix up the opposite name to deceive GitLab.
                     trace("rename (discard)", refs, sourceCurrentName, sourceOppositeName, anotherName);
                     return anotherName;
                  }
                  else
                  {
                     // user will re-match
                     trace("rename (re-match)", refs, sourceCurrentName, sourceOppositeName, String.Empty);
                     return null;
                  }
               }
            }
            else
            {
               if (sourceCurrentName != sourceOppositeName)
               {
                  _onWrongMatch();
                  trace("wrong match", refs, sourceCurrentName, sourceOppositeName, String.Empty);
                  return null;
               }

               // it is not a rename but really modified file detected
               return sourceOppositeName;
            }
         }
      }

      private void trace(string action, DiffRefs refs, string sourceCurrentName, string sourceOppositeName,
         string fixedOppositeName)
      {
         Trace.TraceInformation(String.Format(
            "[MatchInfoCorrector] {0}. Git repository path: {1}. DiffRefs: {2}\n"
          + "sourceCurrentName: {3}\nsourceOppositeName: {4}\nfixedOppositeName: {5}",
               action, _gitRepository.Path, refs.ToString(), sourceCurrentName, sourceOppositeName, fixedOppositeName));
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

