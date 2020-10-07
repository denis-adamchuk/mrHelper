﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GitLabSharp.Entities;
using mrHelper.StorageSupport;
using Newtonsoft.Json;

namespace mrHelper.App.Helpers
{
   public static class ConfigurationHelper
   {
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
         if (Program.Settings.DiscussionColumnWidth == "narrow")
         {
            return DiscussionColumnWidth.Narrow;
         }
         if (Program.Settings.DiscussionColumnWidth == "narrowplus")
         {
            return DiscussionColumnWidth.NarrowPlus;
         }
         else if (Program.Settings.DiscussionColumnWidth == "medium")
         {
            return DiscussionColumnWidth.Medium;
         }
         else if (Program.Settings.DiscussionColumnWidth == "mediumplus")
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
               Program.Settings.DiscussionColumnWidth = "narrow";
               break;

            case DiscussionColumnWidth.NarrowPlus:
               Program.Settings.DiscussionColumnWidth = "narrowplus";
               break;

            case DiscussionColumnWidth.Medium:
               Program.Settings.DiscussionColumnWidth = "medium";
               break;

            case DiscussionColumnWidth.MediumPlus:
               Program.Settings.DiscussionColumnWidth = "mediumplus";
               break;

            case DiscussionColumnWidth.Wide:
               Program.Settings.DiscussionColumnWidth = "wide";
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
         if (Program.Settings.DiffContextPosition == "top")
         {
            return DiffContextPosition.Top;
         }
         else if (Program.Settings.DiffContextPosition == "left")
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
               Program.Settings.DiffContextPosition = "top";
               break;

            case DiffContextPosition.Left:
               Program.Settings.DiffContextPosition = "left";
               break;

            case DiffContextPosition.Right:
               Program.Settings.DiffContextPosition = "right";
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
         if (Program.Settings.ShowWarningsOnFileMismatchMode == "always")
         {
            return ShowWarningsOnFileMismatchMode.Always;
         }
         else if (Program.Settings.ShowWarningsOnFileMismatchMode == "until_user_ignores_file")
         {
            return ShowWarningsOnFileMismatchMode.UntilUserIgnoresFile;
         }
         else
         {
            Debug.Assert(Program.Settings.ShowWarningsOnFileMismatchMode == "never");
            return ShowWarningsOnFileMismatchMode.Never;
         }
      }

      public static void SetShowWarningsOnFileMismatchMode(
         UserDefinedSettings settings, ShowWarningsOnFileMismatchMode mode)
      {
         switch (mode)
         {
            case ShowWarningsOnFileMismatchMode.Always:
               Program.Settings.ShowWarningsOnFileMismatchMode = "always";
               break;

            case ShowWarningsOnFileMismatchMode.UntilUserIgnoresFile:
               Program.Settings.ShowWarningsOnFileMismatchMode = "until_user_ignores_file";
               break;

            case ShowWarningsOnFileMismatchMode.Never:
               Program.Settings.ShowWarningsOnFileMismatchMode = "never";
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
         if (Program.Settings.AutoSelectionMode == "LastVsNext")
         {
            return RevisionAutoSelectionMode.LastVsNext;
         }
         else if (Program.Settings.AutoSelectionMode == "LastVsLatest")
         {
            return RevisionAutoSelectionMode.LastVsLatest;
         }
         else
         {
            Debug.Assert(Program.Settings.AutoSelectionMode == "BaseVsLatest");
            return RevisionAutoSelectionMode.BaseVsLatest;
         }
      }

      public static void SelectAutoSelectionMode(UserDefinedSettings settings, RevisionAutoSelectionMode mode)
      {
         switch (mode)
         {
            case ConfigurationHelper.RevisionAutoSelectionMode.LastVsNext:
               Program.Settings.AutoSelectionMode = "LastVsNext";
               break;

            case ConfigurationHelper.RevisionAutoSelectionMode.LastVsLatest:
               Program.Settings.AutoSelectionMode = "LastVsLatest";
               break;

            case ConfigurationHelper.RevisionAutoSelectionMode.BaseVsLatest:
               Program.Settings.AutoSelectionMode = "BaseVsLatest";
               break;
         }
      }

      public static LocalCommitStorageType GetPreferredStorageType(UserDefinedSettings settings)
      {
         if (Program.Settings.GitUsageForStorage == "UseGitWithFullClone")
         {
            return LocalCommitStorageType.FullGitRepository;
         }
         else if (Program.Settings.GitUsageForStorage == "UseGitWithShallowClone")
         {
            return LocalCommitStorageType.ShallowGitRepository;
         }
         else
         {
            Debug.Assert(Program.Settings.GitUsageForStorage == "DontUseGit");
            return LocalCommitStorageType.FileStorage;
         }
      }

      public static void SelectPreferredStorageType(UserDefinedSettings settings, LocalCommitStorageType type)
      {
         switch (type)
         {
            case LocalCommitStorageType.FileStorage:
               Program.Settings.GitUsageForStorage = "DontUseGit";
               break;
            case LocalCommitStorageType.FullGitRepository:
               Program.Settings.GitUsageForStorage = "UseGitWithFullClone";
               break;
            case LocalCommitStorageType.ShallowGitRepository:
               Program.Settings.GitUsageForStorage = "UseGitWithShallowClone";
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
            .Select(x => new Tuple<string, bool>(x.Item1, bool.TryParse(x.Item2, out bool result) ? result : false));
      }

      public static IEnumerable<Tuple<string, bool>> GetProjectsForHost(string host, UserDefinedSettings settings)
      {
         return DictionaryStringHelper.GetDictionaryStringValue(host, settings.SelectedProjects)
            .Select(x => new Tuple<string, bool>(x.Item1, bool.TryParse(x.Item2, out bool result) ? result : false));
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
   }
}

