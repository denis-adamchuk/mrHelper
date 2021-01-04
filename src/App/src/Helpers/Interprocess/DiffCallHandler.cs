using System;
using System.Linq;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp.Entities;
using GitLabSharp.Accessors;
using mrHelper.App.Forms;
using mrHelper.App.Helpers;
using mrHelper.App.Helpers.GitLab;
using mrHelper.Core.Matching;
using mrHelper.Core.Context;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;
using mrHelper.StorageSupport;
using mrHelper.GitLabClient;
using static mrHelper.App.Helpers.ConfigurationHelper;

namespace mrHelper.App.Interprocess
{
   internal class DiffCallHandler
   {
      internal DiffCallHandler(IGitCommandService git, IModificationListener modificationListener,
         User currentUser, Action<MergeRequestKey> onDiscussionSubmitted,
         Func<MergeRequestKey, IEnumerable<Discussion>> getDiscussions)
      {
         _git = git ?? throw new ArgumentException("git argument cannot be null");
         _modificationListener = modificationListener;
         _currentUser = currentUser;
         _onDiscussionSubmitted = onDiscussionSubmitted;
         _getDiscussions = getDiscussions;
      }

      public void Handle(MatchInfo matchInfo, Snapshot snapshot)
      {
         if (!matchInfo.IsValid())
         {
            throw new ArgumentException(
               String.Format("Bad match info: {0}", matchInfo.ToString()));
         }

         MergeRequestKey mrk = getMergeRequestKey(snapshot);
         if (doFullMatch(mrk, snapshot.Refs, matchInfo, out DiffPosition position, out FullContextDiff fullContextDiff)
            == MatchResult.Error)
         {
            MessageBox.Show("Cannot create a discussion. Unexpected file name and/or line number passed",
               "Error", MessageBoxButtons.OK, MessageBoxIcon.Error,
               MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
            return;
         }

         showNewDiscussionDialog(matchInfo, snapshot, mrk, position, fullContextDiff);
      }

      private void showNewDiscussionDialog(MatchInfo matchInfo, Snapshot snapshot, MergeRequestKey mrk,
         DiffPosition initialNewDiscussionPosition, FullContextDiff fullContextDiff)
      {
         DiffPosition fnOnScroll(DiffPosition position, bool scrollUp) =>
            scrollPosition(position, scrollUp, matchInfo.IsLeftSideLineNumber);

         async Task fnOnSubmitNewDiscussion(string body, bool includeContext, DiffPosition position) =>
            await actOnGitLab(snapshot, "create", (gli) =>
               submitDiscussionAsync(mrk, gli, matchInfo, snapshot.Refs, position, body, includeContext));

         async Task fnOnEditOldNote(ReportedDiscussionNoteKey notePosition, ReportedDiscussionNoteContent content) =>
            await actOnGitLab(snapshot, "edit", (gli) =>
               editDiscussionNoteAsync(mrk, gli, notePosition.DiscussionId, notePosition.Id, content.Body, snapshot));

         async Task fnOnDeleteOldNote(ReportedDiscussionNoteKey notePosition) =>
            await actOnGitLab(snapshot, "delete", (gli) =>
               deleteDiscussionNoteAsync(mrk, gli, notePosition.DiscussionId, notePosition.Id, snapshot));

         IEnumerable<ReportedDiscussionNote> fnGetRelatedDiscussions(ReportedDiscussionNoteKey? keyOpt, DiffPosition position) =>
            getRelatedDiscussions(fullContextDiff, mrk, keyOpt, position).ToArray();

         // We need separate functors for `new` and `old` positions because for a new discussion we need
         // to distinct cases when user points at unchanged line at left and right sides because it is
         // important to show proper file content in a diff context window.
         // And for old discussions we don't have a chance to guess at what side they were created.
         DiffContext fnGetNewDiscussionDiffContext(DiffPosition position) =>
            getDiffContext<EnhancedContextMaker>(position,
                matchInfo.IsLeftSideLineNumber ? UnchangedLinePolicy.TakeFromLeft : UnchangedLinePolicy.TakeFromRight);

         DiffContext fnGetDiffContext(DiffPosition position) =>
            getDiffContext<EnhancedContextMaker>(position, UnchangedLinePolicy.TakeFromRight);

         void fnOnDialogClosed() =>
            _onDiscussionSubmitted?.Invoke(mrk);

         ReportedDiscussionNote[] reportedDiscussions = getReportedDiscussions(mrk).ToArray();

         NewDiscussionForm form = new NewDiscussionForm(
            initialNewDiscussionPosition,
            reportedDiscussions,
            fnOnScroll,
            fnOnDialogClosed,
            fnOnSubmitNewDiscussion,
            fnOnEditOldNote,
            fnOnDeleteOldNote,
            fnGetRelatedDiscussions,
            fnGetNewDiscussionDiffContext,
            fnGetDiffContext);
         form.Show();
      }

      DiffPosition scrollPosition(DiffPosition position, bool scrollUp, bool isLeftSideLine)
      {
         if (!Core.Context.Helpers.IsValidPosition(position))
         {
            return position;
         }

         int lineNumber = isLeftSideLine
               ? Core.Context.Helpers.GetLeftLineNumber(position)
               : Core.Context.Helpers.GetRightLineNumber(position);
         lineNumber += (scrollUp ? -1 : 1);

         string leftPath = position.LeftPath;
         string rightPath = position.RightPath;
         Core.Matching.DiffRefs refs = position.Refs;
         if (matchLineNumber(leftPath, rightPath, refs, lineNumber, isLeftSideLine,
               out string leftLineNumber, out string rightLineNumber) == MatchResult.Success)
         {
            return new DiffPosition(leftPath, rightPath, leftLineNumber, rightLineNumber, refs);
         }
         return null;
      }

      private enum MatchResult
      {
         Success,
         Error,
         Cancelled
      }

      private MatchResult doFullMatch(MergeRequestKey mrk, Core.Matching.DiffRefs refs,
         MatchInfo matchInfo, out DiffPosition position, out FullContextDiff fullContextDiff)
      {
         MatchResult fileMatchResult = matchFileName(mrk, refs, matchInfo.LeftFileName, matchInfo.RightFileName,
            matchInfo.IsLeftSideLineNumber, out string leftFileName, out string rightFileName);
         if (fileMatchResult != MatchResult.Success)
         {
            position = null;
            return fileMatchResult;
         }

         MatchResult lineMatchResult = matchLineNumber(leftFileName, rightFileName, refs, matchInfo.LineNumber,
            matchInfo.IsLeftSideLineNumber, out string leftLineNumber, out string rightLineNumber);
         if (lineMatchResult != MatchResult.Success)
         {
            position = null;
            return fileMatchResult;
         }

         position = new DiffPosition(leftFileName, rightFileName, leftLineNumber, rightLineNumber, refs);
         return MatchResult.Success;
      }

      private MatchResult matchFileName(MergeRequestKey mrk, Core.Matching.DiffRefs refs,
         string originalLeftFileName, string originalRightFileName, bool isLeftSideLine,
         out string leftFileName, out string rightFileName)
      {
         FileNameMatcher fileNameMatcher = getFileNameMatcher(_git, mrk);
         try
         {
            if (!fileNameMatcher.Match(refs, originalLeftFileName, originalRightFileName, isLeftSideLine,
               out leftFileName, out rightFileName))
            {
               return MatchResult.Cancelled;
            }
         }
         catch (Exception ex)
         {
            if (ex is ArgumentException || ex is MatchingException)
            {
               leftFileName = null;
               rightFileName = null;
               ExceptionHandlers.Handle("Cannot create DiffPosition", ex);
               return MatchResult.Error;
            }
            throw;
         }
         return MatchResult.Success;
      }

      private MatchResult matchLineNumber(string leftPath, string rightPath, Core.Matching.DiffRefs refs,
         int lineNumber, bool isLeftSideLine, out string leftLineNumber, out string rightLineNumber)
      {
         LineNumberMatcher matcher = new LineNumberMatcher(_git);
         try
         {
            matcher.Match(refs, leftPath, rightPath, lineNumber, isLeftSideLine, out leftLineNumber, out rightLineNumber);
         }
         catch (Exception ex)
         {
            if (ex is ArgumentException || ex is MatchingException)
            {
               leftLineNumber = null;
               rightLineNumber = null;
               ExceptionHandlers.Handle("Cannot create DiffPosition", ex);
               return MatchResult.Error;
            }
            throw;
         }
         return MatchResult.Success;
      }

      private DiffContext getDiffContext<T>(DiffPosition position, UnchangedLinePolicy policy) where T : IContextMaker
      {
         if (position == null || !Core.Context.Helpers.IsValidPosition(position))
         {
            return DiffContext.InvalidContext;
         }

         try
         {
            T maker = (T)Activator.CreateInstance(typeof(T), _git);
            return maker.GetContext(position, DiffContextDepth, policy);
         }
         catch (Exception ex)
         {
            if (ex is ArgumentException || ex is ContextMakingException)
            {
               ExceptionHandlers.Handle("Failed to obtain DiffContext", ex);
               return DiffContext.InvalidContext;
            }
            throw;
         }
      }

      private IEnumerable<ReportedDiscussionNote> getReportedDiscussions(MergeRequestKey mrk)
      {
         DiscussionFilter discussionFilter = new DiscussionFilter(
            _currentUser, null, DiscussionFilterState.CurrentUserOnly);
         return _getDiscussions(mrk)
            .Where(discussion => discussionFilter.DoesMatchFilter(discussion))
            .Select(discussion =>
            {
               DiscussionNote firstNote = discussion.Notes.First();
               Core.Matching.DiffPosition firstNotePosition = PositionConverter.Convert(firstNote.Position);
               return new ReportedDiscussionNote(firstNote.Id, discussion.Id, firstNotePosition,
                  firstNote.Body, firstNote.Author.Name);
            });
      }

      bool doPositionsReferenceTheSameLine(DiffPosition position1, DiffPosition position2)
      {
         bool matchRightWithRight =
                position1.RightLine != null
             && position1.RightLine == position2.RightLine
             && position1.RightPath == position2.RightPath;
         bool matchLeftWithLeft =
                   position1.LeftLine != null
                && position1.LeftLine == position2.LeftLine
                && position1.LeftPath == position2.LeftPath;
         return matchRightWithRight || matchLeftWithLeft;
      }

      // Collect discussions started for lines within DiffContextDepth range near `position`
      private IEnumerable<ReportedDiscussionNote> getRelatedDiscussions(
         FullContextDiff fullContextDiff, MergeRequestKey mrk, ReportedDiscussionNoteKey? keyOpt, DiffPosition position)
      {
         // Obtain a context for a passed position.
         DiffContext ctx = getDiffContext<CombinedContextMaker>(position, UnchangedLinePolicy.TakeFromRight);
         if (!ctx.IsValid())
         {
            return Array.Empty<ReportedDiscussionNote>();
         }

         string leftFileName = position.LeftPath;
         string rightFileName = position.RightPath;
         Core.Matching.DiffRefs refs = position.Refs;
         List<DiffPosition> neighborPositions = new List<DiffPosition>();

         // CombinedContextMaker provides a context where line numbers are matched so no need to
         // match them manually here.
         foreach (DiffContext.Line line in ctx.Lines)
         {
            Debug.Assert(line.Left.HasValue || line.Right.HasValue);
            string leftLineNumber = null;
            string rightLineNumber = null;
            if (line.Left.HasValue)
            {
               leftLineNumber = line.Left.Value.Number.ToString();
            }
            if (line.Right.HasValue)
            {
               rightLineNumber = line.Right.Value.Number.ToString();
            }
            neighborPositions.Add(new DiffPosition(leftFileName, rightFileName, leftLineNumber, rightLineNumber, refs));
         }

         // Find discussions that reported on each line from the diff context.
         List<ReportedDiscussionNote> relatedNotes = new List<ReportedDiscussionNote>();
         foreach (Discussion discussion in _getDiscussions(mrk))
         {
            DiscussionNote firstNote = discussion.Notes.First();
            DiffPosition firstNotePosition = PositionConverter.Convert(firstNote.Position);
            if (firstNotePosition != null)
            {
               foreach (DiffPosition neighbor in neighborPositions)
               {
                  if (doPositionsReferenceTheSameLine(neighbor, firstNotePosition)
                     && (!keyOpt.HasValue || keyOpt.Value.Id != firstNote.Id))
                  {
                     ReportedDiscussionNote note = new ReportedDiscussionNote(firstNote.Id, discussion.Id,
                        firstNotePosition, firstNote.Body, firstNote.Author.Name);
                     relatedNotes.Add(note);
                  }
               }
            }
         }
         return relatedNotes.GroupBy(note => note.Key.Id).Select(c => c.First());
      }

      private FileNameMatcher getFileNameMatcher(IGitCommandService git, MergeRequestKey mrk)
      {
         return new FileNameMatcher(git,
            (currentName, anotherName) =>
         {
            MessageBox.Show(
               "Merge Request Helper detected that current file is a moved version of another file. "
               + "GitLab does not allow to create discussions on moved files.\n\n"
               + "Current file:\n"
               + currentName + "\n\n"
               + "Another file:\n"
               + anotherName,
               "Cannot create a discussion",
               MessageBoxButtons.OK, MessageBoxIcon.Warning,
               MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
         },
            (currentName, anotherName, status) =>
         {
            if (needSuppressWarning(currentName, mrk))
            {
               return true;
            }

            string question = String.Empty;
            if (status == "new" || status == "deleted")
            {
               question = "Do you really want to review this file as a " + status + " file? ";
            }
            else if (status == "modified")
            {
               question = "Do you really want to continue reviewing this file against the selected file? ";
            }
            else
            {
               Debug.Assert(false);
            }

            bool isWarningIgnoredByUser = MessageBox.Show(
                  "Merge Request Helper detected that current file is a renamed version of another file. "
                  + question
                  + "It is recommended to press \"No\" and match files manually in the diff tool.\n"
                  + "Current file:\n"
                  + currentName + "\n\n"
                  + "Another file:\n"
                  + anotherName,
                  "Rename detected",
                  MessageBoxButtons.YesNo, MessageBoxIcon.Information,
                  MessageBoxDefaultButton.Button2, MessageBoxOptions.ServiceNotification)
               == DialogResult.Yes;
            if (isWarningIgnoredByUser)
            {
               addFileToWhitelist(currentName, mrk);
            }
            return isWarningIgnoredByUser;
         },
            (currentName) =>
         {
            if (needSuppressWarning(currentName, mrk))
            {
               return true;
            }

            string question = "Do you really want to continue reviewing this file against the selected file? ";
            bool isWarningIgnoredByUser = MessageBox.Show(
                  "Merge Request Helper detected that selected files do not match to each other. "
                  + question, "Files do not match",
                  MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                  MessageBoxDefaultButton.Button2, MessageBoxOptions.ServiceNotification)
               == DialogResult.Yes;
            if (isWarningIgnoredByUser)
            {
               addFileToWhitelist(currentName, mrk);
            }
            return isWarningIgnoredByUser;
         });
      }

      async private Task actOnGitLab(Snapshot snapshot, string actionName, Func<GitLabInstance, Task> action)
      {
         try
         {
            await action(new GitLabInstance(snapshot.Host, Program.Settings));
         }
         catch (Exception ex)
         {
            Debug.Assert(ex is DiscussionEditorException || ex is DiscussionCreatorException);
            string message = String.Format("Cannot {0} a discussion at GitLab", actionName);
            ExceptionHandlers.Handle(message, ex);
            MessageBox.Show(String.Format("{0}. Check your connection and try again.", message),
               "Error", MessageBoxButtons.OK, MessageBoxIcon.Error,
               MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
         }
      }

      async private Task submitDiscussionAsync(MergeRequestKey mrk, GitLabInstance gitLabInstance,
         MatchInfo matchInfo, Core.Matching.DiffRefs diffRefs, DiffPosition position, string body, bool includeContext)
      {
         if (body.Length == 0)
         {
            MessageBox.Show("Discussion text cannot be empty", "Warning",
               MessageBoxButtons.OK, MessageBoxIcon.Exclamation,
               MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
            return;
         }

         NewDiscussionParameters parameters = new NewDiscussionParameters(
            body, includeContext ? createPositionParameters(position) : new PositionParameters?());

         IDiscussionCreator creator = Shortcuts.GetDiscussionCreator(
            gitLabInstance, _modificationListener, mrk, _currentUser);

         try
         {
            await creator.CreateDiscussionAsync(parameters, true);
         }
         catch (DiscussionCreatorException ex)
         {
            Trace.TraceInformation(
                  "Additional information about exception:\n" +
                  "Position: {0}\n" +
                  "Include context: {1}\n" +
                  "Snapshot refs: {2}\n" +
                  "MatchInfo: {3}\n" +
                  "Body:\n{4}",
                  position.ToString(),
                  includeContext.ToString(),
                  diffRefs.ToString(),
                  matchInfo.ToString(),
                  body);

            if (!ex.Handled)
            {
               throw;
            }
         }
      }

      private Task editDiscussionNoteAsync(MergeRequestKey mrk, GitLabInstance gitLabInstance,
         string discussionId, int noteId, string text, Snapshot snapshot)
      {
         IDiscussionEditor editor = Shortcuts.GetDiscussionEditor(
            gitLabInstance, _modificationListener, mrk, discussionId);
         return editor.ModifyNoteBodyAsync(noteId, text);
      }

      private Task deleteDiscussionNoteAsync(MergeRequestKey mrk, GitLabInstance gitLabInstance,
         string discussionId, int noteId, Snapshot snapshot)
      {
         IDiscussionEditor editor = Shortcuts.GetDiscussionEditor(
            gitLabInstance, _modificationListener, mrk, discussionId);
         return editor.DeleteNoteAsync(noteId);
      }

      private static PositionParameters createPositionParameters(DiffPosition position)
      {
         return new PositionParameters(position.LeftPath, position.RightPath, position.LeftLine,
            position.RightLine, position.Refs.LeftSHA, position.Refs.RightSHA, position.Refs.LeftSHA);
      }

      private bool needSuppressWarning(string filename, MergeRequestKey mrk)
      {
         switch (GetShowWarningsOnFileMismatchMode(Program.Settings))
         {
            case ShowWarningsOnFileMismatchMode.Always:
               return false;

            case ShowWarningsOnFileMismatchMode.Never:
               return true;

            case ShowWarningsOnFileMismatchMode.UntilUserIgnoresFile:
               {
                  MismatchWhitelistKey key = new MismatchWhitelistKey(mrk, filename);
                  return _mismatchWhitelist.Contains(key);
               }
         }

         Debug.Assert(false);
         return false;
      }

      private void addFileToWhitelist(string filename, MergeRequestKey mrk)
      {
         MismatchWhitelistKey key = new MismatchWhitelistKey(mrk, filename);
         _mismatchWhitelist.Add(key);
      }

      private static MergeRequestKey getMergeRequestKey(Snapshot snapshot)
      {
         ProjectKey projectKey = new ProjectKey(snapshot.Host, snapshot.Project);
         return new MergeRequestKey(projectKey, snapshot.MergeRequestIId);
      }

      private readonly IGitCommandService _git;
      private readonly IModificationListener _modificationListener;
      private readonly User _currentUser;
      private readonly Action<MergeRequestKey> _onDiscussionSubmitted;
      private readonly Func<MergeRequestKey, IEnumerable<Discussion>> _getDiscussions;

      private struct MismatchWhitelistKey : IEquatable<MismatchWhitelistKey>
      {
         public MismatchWhitelistKey(MergeRequestKey mergeRequestKey, string fileName) : this()
         {
            MergeRequestKey = mergeRequestKey;
            FileName = fileName;
         }

         internal MergeRequestKey MergeRequestKey { get; }
         internal string FileName { get; }

         public override bool Equals(object obj)
         {
            return obj is MismatchWhitelistKey && Equals((MismatchWhitelistKey)obj);
         }

         public bool Equals(MismatchWhitelistKey other)
         {
            return MergeRequestKey.Equals(other.MergeRequestKey) &&
                   FileName == other.FileName;
         }

         public override int GetHashCode()
         {
            var hashCode = 1704511527;
            hashCode = hashCode * -1521134295 + EqualityComparer<MergeRequestKey>.Default.GetHashCode(MergeRequestKey);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(FileName);
            return hashCode;
         }
      }

      private static readonly HashSet<MismatchWhitelistKey> _mismatchWhitelist = new HashSet<MismatchWhitelistKey>();

      private static readonly ContextDepth DiffContextDepth = new ContextDepth(2, 2);
   }
}

