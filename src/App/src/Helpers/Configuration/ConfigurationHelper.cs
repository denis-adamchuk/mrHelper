using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GitLabSharp.Entities;
using mrHelper.Common.Constants;
using mrHelper.StorageSupport;
using Newtonsoft.Json;

namespace mrHelper.App.Helpers
{
   public static class ConfigurationHelper
   {
      public static void SetAuthInfo(IEnumerable<Tuple<string, string>> hostToken, UserDefinedSettings settings)
      {
         settings.KnownHosts = hostToken.Select(tuple => tuple.Item1).ToArray();
         settings.KnownAccessTokens = hostToken.Select(tuple => tuple.Item2).ToArray();
      }

      public enum DiscussionColumnWidth
      {
         Narrow,
         NarrowPlus,
         Medium,
         MediumPlus,
         Wide
      }

      public static DiscussionColumnWidth GetNextColumnWidth(DiscussionColumnWidth value)
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

      public static DiscussionColumnWidth GetPrevColumnWidth(DiscussionColumnWidth value)
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

      public static DiscussionColumnWidth GetDiscussionColumnWidth(UserDefinedSettings settings)
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

      public static void SetDiscussionColumnWidth(UserDefinedSettings settings, DiscussionColumnWidth width)
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

      public enum DiffContextPosition
      {
         Top,
         Left,
         Right
      }

      public static DiffContextPosition GetDiffContextPosition(UserDefinedSettings settings)
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

      public static void SetDiffContextPosition(UserDefinedSettings settings, DiffContextPosition position)
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

      public static int GetDiffContextDepth(UserDefinedSettings settings)
      {
         return int.TryParse(settings.DiffContextDepth, out int result) ? result : 2;
      }

      public enum ShowWarningsOnFileMismatchMode
      {
         Always,
         UntilUserIgnoresFile,
         Never
      }

      public static ShowWarningsOnFileMismatchMode GetShowWarningsOnFileMismatchMode(UserDefinedSettings settings)
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

      public static void SetShowWarningsOnFileMismatchMode(
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

      public enum RevisionAutoSelectionMode
      {
         LastVsNext,
         LastVsLatest,
         BaseVsLatest
      }

      public static RevisionAutoSelectionMode GetRevisionAutoSelectionMode(UserDefinedSettings settings)
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

      public static void SelectAutoSelectionMode(UserDefinedSettings settings, RevisionAutoSelectionMode mode)
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

      public static LocalCommitStorageType GetPreferredStorageType(UserDefinedSettings settings)
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

      public static void SelectPreferredStorageType(UserDefinedSettings settings, LocalCommitStorageType type)
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

      public static RevisionType GetDefaultRevisionType(UserDefinedSettings settings)
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

      public static void SelectRevisionType(UserDefinedSettings settings, RevisionType type)
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

      public static string[] GetDisplayFilterKeywords(UserDefinedSettings settings)
      {
         return settings.DisplayFilter
            .Split(',')
            .Select(x => x.Trim(' '))
            .ToArray();
      }

      public class HostInProjectsFile
      {
         public HostInProjectsFile(string hostname, IEnumerable<Project> projects)
         {
            Name = hostname;
            Projects = projects;
         }

         [JsonProperty]
         public string Name { get; protected set; }

         [JsonProperty]
         public IEnumerable<Project> Projects { get; protected set; }
      }

      public static void InitializeSelectedProjects(IEnumerable<HostInProjectsFile> projects,
         UserDefinedSettings settings)
      {
         if (projects == null)
         {
            return;
         }

         projects = projects
            .Where(x => !String.IsNullOrEmpty(x.Name) && (x.Projects?.Any() ?? false));
         if (!projects.Any())
         {
            return;
         }

         Dictionary<string, string> selectedProjects = settings.SelectedProjects;
         DictionaryStringHelper.UpdateRawDictionaryString(
            projects.ToDictionary(
               x => x.Name,
               x => x.Projects.Select(y => new Tuple<string, string>(y.Path_With_Namespace, bool.TrueString))),
            selectedProjects);
         settings.SelectedProjects = selectedProjects;
      }

      public static void SetUsersForHost(string host, IEnumerable<Tuple<string, bool>> users,
         UserDefinedSettings settings)
      {
         Dictionary<string, string> selectedUsers = settings.SelectedUsers;
         DictionaryStringHelper.UpdateRawDictionaryString(
            new Dictionary<string, IEnumerable<Tuple<string, string>>>
            {
               { host, users.Select(y => new Tuple<string, string>(y.Item1, y.Item2.ToString())) }
            },
            selectedUsers);
         settings.SelectedUsers = selectedUsers;
      }

      public static void SetProjectsForHost(string host, IEnumerable<Tuple<string, bool>> projects,
         UserDefinedSettings settings)
      {
         Dictionary<string, string> selectedProjects = settings.SelectedProjects;
         DictionaryStringHelper.UpdateRawDictionaryString(
            new Dictionary<string, IEnumerable<Tuple<string, string>>>
            {
               { host, projects.Select(y => new Tuple<string, string>(y.Item1, y.Item2.ToString())) }
            },
            selectedProjects);
         settings.SelectedProjects = selectedProjects;
      }

      public static IEnumerable<Tuple<string, bool>> GetUsersForHost(string host, UserDefinedSettings settings)
      {
         return DictionaryStringHelper.GetDictionaryStringValue(host, settings.SelectedUsers)
            .Select(x => new Tuple<string, bool>(x.Item1, bool.TryParse(x.Item2, out bool result) && result));
      }

      public static IEnumerable<Tuple<string, bool>> GetProjectsForHost(string host, UserDefinedSettings settings)
      {
         return DictionaryStringHelper.GetDictionaryStringValue(host, settings.SelectedProjects)
            .Select(x => new Tuple<string, bool>(x.Item1, bool.TryParse(x.Item2, out bool result) && result));
      }

      public static IEnumerable<string> GetEnabledProjectNames(string hostname, UserDefinedSettings settings)
      {
         return GetProjectsForHost(hostname, settings)
            .Where(x => x.Item2).Select(x => x.Item1);
      }

      public static IEnumerable<string> GetEnabledUsers(string hostname, UserDefinedSettings settings)
      {
         return GetUsersForHost(hostname, settings).Where(x => x.Item2)?.Select(x => x.Item1);
      }

      public static void SelectProjectBasedWorkflow(UserDefinedSettings settings)
         => settings.WorkflowType = "Projects";

      public static void SelectUserBasedWorkflow(UserDefinedSettings settings)
         => settings.WorkflowType = "Users";

      public static bool IsProjectBasedWorkflowSelected(UserDefinedSettings settings)
         => settings.WorkflowType == "Projects";

      public static Dictionary<string, int> GetColumnWidths(UserDefinedSettings settings, string listViewName)
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
         Debug.Assert(false);
         return null;
      }

      public static void SetColumnWidths(UserDefinedSettings settings, Dictionary<string, int> widths, string listViewName)
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

      public static Dictionary<string, int> GetColumnIndices(UserDefinedSettings settings, string listViewName)
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
         Debug.Assert(false);
         return null;
      }

      public static void SetColumnIndices(UserDefinedSettings settings, Dictionary<string, int> indices, string listViewName)
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
   }
}

