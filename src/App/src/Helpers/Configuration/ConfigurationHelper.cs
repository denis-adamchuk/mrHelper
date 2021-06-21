using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using mrHelper.Common.Constants;
using mrHelper.StorageSupport;

namespace mrHelper.App.Helpers
{
   internal static class ConfigurationHelper
   {
      internal static void SetAuthInfo(IEnumerable<Tuple<string, string>> hostToken, UserDefinedSettings settings)
      {
         settings.KnownHosts = hostToken.Select(tuple => tuple.Item1).ToArray();
         settings.KnownAccessTokens = hostToken.Select(tuple => tuple.Item2).ToArray();
      }

      internal enum DiscussionColumnWidth
      {
         Narrow,
         NarrowPlus,
         Medium,
         MediumPlus,
         Wide
      }

      internal static DiscussionColumnWidth GetNextColumnWidth(DiscussionColumnWidth value)
      {
         switch (value)
         {
            case DiscussionColumnWidth.Narrow:     return DiscussionColumnWidth.NarrowPlus;
            case DiscussionColumnWidth.NarrowPlus: return DiscussionColumnWidth.Medium;
            case DiscussionColumnWidth.Medium:     return DiscussionColumnWidth.MediumPlus;
            case DiscussionColumnWidth.MediumPlus: return DiscussionColumnWidth.Wide;
            case DiscussionColumnWidth.Wide:       return DiscussionColumnWidth.Wide;
            default: Debug.Assert(false);          return DiscussionColumnWidth.Wide;
         }
      }

      internal static DiscussionColumnWidth GetPrevColumnWidth(DiscussionColumnWidth value)
      {
         switch (value)
         {
            case DiscussionColumnWidth.Narrow:     return DiscussionColumnWidth.Narrow;
            case DiscussionColumnWidth.NarrowPlus: return DiscussionColumnWidth.Narrow;
            case DiscussionColumnWidth.Medium:     return DiscussionColumnWidth.NarrowPlus;
            case DiscussionColumnWidth.MediumPlus: return DiscussionColumnWidth.Medium;
            case DiscussionColumnWidth.Wide:       return DiscussionColumnWidth.MediumPlus;
            default: Debug.Assert(false);          return DiscussionColumnWidth.Narrow;
         }
      }

      internal static DiscussionColumnWidth GetDiscussionColumnWidth(UserDefinedSettings settings)
      {
         if (settings.DiscussionColumnWidth == "narrow")
         {
            return DiscussionColumnWidth.Narrow;
         }
         if (settings.DiscussionColumnWidth == "narrowplus")
         {
            return DiscussionColumnWidth.NarrowPlus;
         }
         else if (settings.DiscussionColumnWidth == "medium")
         {
            return DiscussionColumnWidth.Medium;
         }
         else if (settings.DiscussionColumnWidth == "mediumplus")
         {
            return DiscussionColumnWidth.MediumPlus;
         }
         else
         {
            return DiscussionColumnWidth.Wide;
         }
      }

      internal static void SetDiscussionColumnWidth(UserDefinedSettings settings, DiscussionColumnWidth width)
      {
         switch (width)
         {
            case DiscussionColumnWidth.Narrow:
               settings.DiscussionColumnWidth = "narrow";
               break;

            case DiscussionColumnWidth.NarrowPlus:
               settings.DiscussionColumnWidth = "narrowplus";
               break;

            case DiscussionColumnWidth.Medium:
               settings.DiscussionColumnWidth = "medium";
               break;

            case DiscussionColumnWidth.MediumPlus:
               settings.DiscussionColumnWidth = "mediumplus";
               break;

            case DiscussionColumnWidth.Wide:
               settings.DiscussionColumnWidth = "wide";
               break;
         }
      }

      internal enum DiffContextPosition
      {
         Top,
         Left,
         Right
      }

      internal static DiffContextPosition GetDiffContextPosition(UserDefinedSettings settings)
      {
         if (settings.DiffContextPosition == "top")
         {
            return DiffContextPosition.Top;
         }
         else if (settings.DiffContextPosition == "left")
         {
            return DiffContextPosition.Left;
         }
         else
         {
            return DiffContextPosition.Right;
         }
      }

      internal static void SetDiffContextPosition(UserDefinedSettings settings, DiffContextPosition position)
      {
         switch (position)
         {
            case DiffContextPosition.Top:
               settings.DiffContextPosition = "top";
               break;

            case DiffContextPosition.Left:
               settings.DiffContextPosition = "left";
               break;

            case DiffContextPosition.Right:
               settings.DiffContextPosition = "right";
               break;
         }
      }

      internal enum ShowWarningsOnFileMismatchMode
      {
         Always,
         UntilUserIgnoresFile,
         Never
      }

      internal static ShowWarningsOnFileMismatchMode GetShowWarningsOnFileMismatchMode(UserDefinedSettings settings)
      {
         if (settings.ShowWarningsOnFileMismatchMode == "always")
         {
            return ShowWarningsOnFileMismatchMode.Always;
         }
         else if (settings.ShowWarningsOnFileMismatchMode == "until_user_ignores_file")
         {
            return ShowWarningsOnFileMismatchMode.UntilUserIgnoresFile;
         }
         else
         {
            Debug.Assert(settings.ShowWarningsOnFileMismatchMode == "never");
            return ShowWarningsOnFileMismatchMode.Never;
         }
      }

      internal static void SetShowWarningsOnFileMismatchMode(
         UserDefinedSettings settings, ShowWarningsOnFileMismatchMode mode)
      {
         switch (mode)
         {
            case ShowWarningsOnFileMismatchMode.Always:
               settings.ShowWarningsOnFileMismatchMode = "always";
               break;

            case ShowWarningsOnFileMismatchMode.UntilUserIgnoresFile:
               settings.ShowWarningsOnFileMismatchMode = "until_user_ignores_file";
               break;

            case ShowWarningsOnFileMismatchMode.Never:
               settings.ShowWarningsOnFileMismatchMode = "never";
               break;
         }
      }

      internal enum RevisionAutoSelectionMode
      {
         LastVsNext,
         LastVsLatest,
         BaseVsLatest
      }

      internal static RevisionAutoSelectionMode GetRevisionAutoSelectionMode(UserDefinedSettings settings)
      {
         if (settings.AutoSelectionMode == "LastVsNext")
         {
            return RevisionAutoSelectionMode.LastVsNext;
         }
         else if (settings.AutoSelectionMode == "LastVsLatest")
         {
            return RevisionAutoSelectionMode.LastVsLatest;
         }
         else
         {
            Debug.Assert(settings.AutoSelectionMode == "BaseVsLatest");
            return RevisionAutoSelectionMode.BaseVsLatest;
         }
      }

      internal static void SelectAutoSelectionMode(UserDefinedSettings settings, RevisionAutoSelectionMode mode)
      {
         switch (mode)
         {
            case ConfigurationHelper.RevisionAutoSelectionMode.LastVsNext:
               settings.AutoSelectionMode = "LastVsNext";
               break;

            case ConfigurationHelper.RevisionAutoSelectionMode.LastVsLatest:
               settings.AutoSelectionMode = "LastVsLatest";
               break;

            case ConfigurationHelper.RevisionAutoSelectionMode.BaseVsLatest:
               settings.AutoSelectionMode = "BaseVsLatest";
               break;
         }
      }

      internal static LocalCommitStorageType GetPreferredStorageType(UserDefinedSettings settings)
      {
         if (settings.GitUsageForStorage == "UseGitWithFullClone")
         {
            return LocalCommitStorageType.FullGitRepository;
         }
         else if (settings.GitUsageForStorage == "UseGitWithShallowClone")
         {
            return LocalCommitStorageType.ShallowGitRepository;
         }
         else
         {
            Debug.Assert(settings.GitUsageForStorage == "DontUseGit");
            return LocalCommitStorageType.FileStorage;
         }
      }

      internal static void SelectPreferredStorageType(UserDefinedSettings settings, LocalCommitStorageType type)
      {
         switch (type)
         {
            case LocalCommitStorageType.FileStorage:
               settings.GitUsageForStorage = "DontUseGit";
               break;
            case LocalCommitStorageType.FullGitRepository:
               settings.GitUsageForStorage = "UseGitWithFullClone";
               break;
            case LocalCommitStorageType.ShallowGitRepository:
               settings.GitUsageForStorage = "UseGitWithShallowClone";
               break;
         }
      }

      internal static RevisionType GetDefaultRevisionType(UserDefinedSettings settings)
      {
         if (settings.RevisionType == "Commit")
         {
            return RevisionType.Commit;
         }
         else
         {
            Debug.Assert(settings.RevisionType == "Version");
            return RevisionType.Version;
         }
      }

      internal static void SelectRevisionType(UserDefinedSettings settings, RevisionType type)
      {
         switch (type)
         {
            case RevisionType.Commit:
               settings.RevisionType = "Commit";
               break;

            case RevisionType.Version:
               settings.RevisionType = "Version";
               break;
         }
      }

      internal static string[] GetDisplayFilterKeywords(UserDefinedSettings settings)
      {
         return settings.DisplayFilter
            .Split(',')
            .Select(x => x.Trim(' '))
            .ToArray();
      }

      internal class StringToBooleanCollection : List<Tuple<string, bool>>
      {
         internal StringToBooleanCollection()
            : base(new List<Tuple<string, bool>>())
         {
         }

         internal StringToBooleanCollection(IEnumerable<Tuple<string, bool>> collection) : base(collection)
         {
         }
      }

      internal static void SetUsersForHost(string host, StringToBooleanCollection users,
         UserDefinedSettings settings)
      {
         Dictionary<string, string> selectedUsers = settings.SelectedUsers;
         HostStringHelper.UpdateRawDictionaryString(
            new Dictionary<string, IEnumerable<Tuple<string, string>>>
            {
               { host, users.Select(y => new Tuple<string, string>(y.Item1, y.Item2.ToString())) }
            },
            selectedUsers);
         settings.SelectedUsers = selectedUsers;
      }

      internal static void SetProjectsForHost(string host, StringToBooleanCollection projects,
         UserDefinedSettings settings)
      {
         Dictionary<string, string> selectedProjects = settings.SelectedProjects;
         HostStringHelper.UpdateRawDictionaryString(
            new Dictionary<string, IEnumerable<Tuple<string, string>>>
            {
               { host, projects.Select(y => new Tuple<string, string>(y.Item1, y.Item2.ToString())) }
            },
            selectedProjects);
         settings.SelectedProjects = selectedProjects;
      }

      internal static StringToBooleanCollection GetUsersForHost(string host, UserDefinedSettings settings)
      {
         return new StringToBooleanCollection(HostStringHelper.GetDictionaryStringValue(host, settings.SelectedUsers)
            .Select(x => new Tuple<string, bool>(x.Item1, bool.TryParse(x.Item2, out bool result) && result)));
      }

      internal static StringToBooleanCollection GetProjectsForHost(string host, UserDefinedSettings settings)
      {
         return new StringToBooleanCollection(HostStringHelper.GetDictionaryStringValue(host, settings.SelectedProjects)
            .Select(x => new Tuple<string, bool>(x.Item1, bool.TryParse(x.Item2, out bool result) && result)));
      }

      internal static IEnumerable<string> GetEnabledProjects(string hostname, UserDefinedSettings settings)
      {
         return GetProjectsForHost(hostname, settings)
            .Where(x => x.Item2).Select(x => x.Item1);
      }

      internal static IEnumerable<string> GetEnabledUsers(string hostname, UserDefinedSettings settings)
      {
         return GetUsersForHost(hostname, settings).Where(x => x.Item2)?.Select(x => x.Item1);
      }

      internal static void SelectCommonWorkflow(UserDefinedSettings settings)
         => settings.WorkflowType = "Common";

      internal static bool IsCommonWorkflowSelected(UserDefinedSettings settings)
         => settings.WorkflowType == "Common";

      internal static bool IsOldProjectBasedWorkflowSelected(UserDefinedSettings settings)
         => settings.WorkflowType == "Projects";

      internal static Dictionary<string, int> GetColumnWidths(UserDefinedSettings settings, string listViewName)
      {
         if (listViewName == Constants.LiveListViewName)
         {
            return settings.ListViewMergeRequestsColumnWidths;
         }
         else if (listViewName == Constants.SearchListViewName)
         {
            return settings.ListViewFoundMergeRequestsColumnWidths;
         }
         else if (listViewName == Constants.RecentListViewName)
         {
            return settings.ListViewRecentMergeRequestsColumnWidths;
         }
         return null;
      }

      internal static void SetColumnWidths(UserDefinedSettings settings, Dictionary<string, int> widths, string listViewName)
      {
         if (listViewName == Constants.LiveListViewName)
         {
            settings.ListViewMergeRequestsColumnWidths = widths;
         }
         else if (listViewName == Constants.SearchListViewName)
         {
            settings.ListViewFoundMergeRequestsColumnWidths = widths;
         }
         else if (listViewName == Constants.RecentListViewName)
         {
            settings.ListViewRecentMergeRequestsColumnWidths = widths;
         }
      }

      internal static Dictionary<string, int> GetColumnIndices(UserDefinedSettings settings, string listViewName)
      {
         if (listViewName == Constants.LiveListViewName)
         {
            return settings.ListViewMergeRequestsDisplayIndices;
         }
         else if (listViewName == Constants.SearchListViewName)
         {
            return settings.ListViewFoundMergeRequestsDisplayIndices;
         }
         else if (listViewName == Constants.RecentListViewName)
         {
            return settings.ListViewRecentMergeRequestsDisplayIndices;
         }
         return null;
      }

      internal static void SetColumnIndices(UserDefinedSettings settings, Dictionary<string, int> indices, string listViewName)
      {
         if (listViewName == Constants.LiveListViewName)
         {
            settings.ListViewMergeRequestsDisplayIndices = indices;
         }
         else if (listViewName == Constants.SearchListViewName)
         {
            settings.ListViewFoundMergeRequestsDisplayIndices = indices;
         }
         else if (listViewName == Constants.RecentListViewName)
         {
            settings.ListViewRecentMergeRequestsDisplayIndices = indices;
         }
      }

      internal enum MainWindowLayout
      {
         Horizontal,
         Vertical
      }

      internal static MainWindowLayout GetMainWindowLayout(UserDefinedSettings settings)
      {
         if (settings.MainWindowLayout == "Horizontal")
         {
            return MainWindowLayout.Horizontal;
         }
         else if (settings.MainWindowLayout == "Vertical")
         {
            return MainWindowLayout.Vertical;
         }
         Debug.Assert(false);
         return MainWindowLayout.Horizontal;
      }

      internal static void SetMainWindowLayout(UserDefinedSettings settings, MainWindowLayout mainWindowLayout)
      {
         switch (mainWindowLayout)
         {
            case MainWindowLayout.Horizontal:
               settings.MainWindowLayout = "Horizontal";
               break;

            case MainWindowLayout.Vertical:
               settings.MainWindowLayout = "Vertical";
               break;
         }
      }
      internal enum ToolBarPosition
      {
         Top,
         Left,
         Right
      }

      internal static ToolBarPosition GetToolBarPosition(UserDefinedSettings settings)
      {
         if (settings.ToolBarPosition == "Top")
         {
            return ToolBarPosition.Top;
         }
         else if (settings.ToolBarPosition == "Left")
         {
            return ToolBarPosition.Left;
         }
         else if (settings.ToolBarPosition == "Right")
         {
            return ToolBarPosition.Right;
         }
         Debug.Assert(false);
         return ToolBarPosition.Right;
      }

      internal static void SetToolBarPosition(UserDefinedSettings settings, ToolBarPosition ToolBarPosition)
      {
         switch (ToolBarPosition)
         {
            case ToolBarPosition.Top:
               settings.ToolBarPosition = "Top";
               break;

            case ToolBarPosition.Left:
               settings.ToolBarPosition = "Left";
               break;

            case ToolBarPosition.Right:
               settings.ToolBarPosition = "Right";
               break;
         }
      }
   }
}

