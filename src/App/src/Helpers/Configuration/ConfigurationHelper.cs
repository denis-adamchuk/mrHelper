using System;
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

      public static IEnumerable<Project> GetEnabledProjects(string hostname, UserDefinedSettings settings)
      {
         return GetProjectsForHost(hostname, settings)
            .Where(x => x.Item2).Select(x => x.Item1).Select(x => new Project(x));
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

