using System;
using System.Diagnostics;
using mrHelper.Core.Git;
using mrHelper.StorageSupport;

namespace mrHelper.Core.Matching
{
   /// <summary>
   /// Fills Paths of DiffPosition
   /// </summary>
   public class FileNameMatcher
   {
      public FileNameMatcher(IGitCommitStorage gitRepository,
         Action<string, string> onFileMove, Func<string, string, string, bool> onFileRename, Func<bool> onWrongMatch)
      {
         _gitRepository = gitRepository;
         _onFileMove = onFileMove;
         _onFileRename = onFileRename;
         _onWrongMatch = onWrongMatch;
      }

      /// <summary>
      /// Throws MatchingException.
      /// </summary>
      public bool Match(MatchInfo matchInfo, DiffPosition inDiffPosition, out DiffPosition outDiffPosition)
      {
         if (!matchInfo.IsValid())
         {
            throw new ArgumentException(
               String.Format("Bad match info: {0}", matchInfo.ToString()));
         }

         bool isLeftSide = matchInfo.IsLeftSideLineNumber;
         string currentName = isLeftSide ? matchInfo.LeftFileName : matchInfo.RightFileName;
         string oppositeName = isLeftSide ? matchInfo.RightFileName : matchInfo.LeftFileName;
         oppositeName = getOppositeName(inDiffPosition.Refs, isLeftSide, currentName, oppositeName);

         outDiffPosition = new DiffPosition(
            isLeftSide ? currentName : oppositeName,
            isLeftSide ? oppositeName : currentName,
            inDiffPosition.LeftLine,
            inDiffPosition.RightLine,
            inDiffPosition.Refs);
         return oppositeName != null;
      }

      /// <summary>
      /// Throws MatchingException.
      /// </summary>
      private string getOppositeName(DiffRefs refs, bool isLeftSide, string sourceCurrentName, string sourceOppositeName)
      {
         Debug.Assert(sourceCurrentName != String.Empty);

         GitRenameDetector renameChecker = new GitRenameDetector(_gitRepository);
         string anotherName = renameChecker.IsRenamed(refs.LeftSHA, refs.RightSHA,
            sourceCurrentName, isLeftSide, out bool moved);
         if (moved)
         {
            _onFileMove(sourceCurrentName, anotherName);
            trace("move", isLeftSide, refs, sourceCurrentName, sourceOppositeName, String.Empty);
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
                  trace("rename (continue)", isLeftSide, refs, sourceCurrentName, sourceOppositeName, anotherName);
                  return anotherName;
               }
               else
               {
                  // user will re-match
                  trace("rename (cancel)", isLeftSide, refs, sourceCurrentName, sourceOppositeName, String.Empty);
                  return null;
               }
            }
            else
            {
               // no rename detected
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
                     trace("rename (continue)", isLeftSide, refs, sourceCurrentName, sourceOppositeName, anotherName);
                     return anotherName;
                  }
                  else
                  {
                     // user will re-match
                     trace("rename (cancel)", isLeftSide, refs, sourceCurrentName, sourceOppositeName, String.Empty);
                     return null;
                  }
               }
            }
            else
            {
               // no rename detected
               if (sourceCurrentName != sourceOppositeName)
               {
                  // wrong match, propose to re-match
                  if (_onWrongMatch())
                  {
                     trace("wrong match (continue)", isLeftSide, refs, sourceCurrentName, sourceOppositeName, sourceCurrentName);
                     return sourceCurrentName;
                  }
                  else
                  {
                     trace("wrong match (cancel)", isLeftSide, refs, sourceCurrentName, sourceOppositeName, String.Empty);
                     return null;
                  }
               }
               else
               {
                  // it is not a rename but really modified file detected
                  return sourceOppositeName;
               }
            }
         }
      }

      private void trace(string action, bool isLeftSide, DiffRefs refs,
         string sourceCurrentName, string sourceOppositeName, string fixedOppositeName)
      {
         Trace.TraceInformation(String.Format(
            "[FileNameMatcher] {0}. Repo: {1}. DiffRefs: {2}\n"
          + "sourceCurrentName: {3}\nsourceOppositeName: {4}\nfixedOppositeName: {5}\nIsLeftSide: {6}",
               action, _gitRepository.ToString(), refs.ToString(),
               sourceCurrentName, sourceOppositeName, fixedOppositeName, isLeftSide));
      }

      private readonly IGitCommitStorage _gitRepository;

      /// <summary>
      /// Notify user about impossible match
      /// </summary>
      private readonly Func<bool> _onWrongMatch;

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

