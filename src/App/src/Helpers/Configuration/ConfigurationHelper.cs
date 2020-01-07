using GitLabSharp.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace mrHelper.App.Helpers
{
   public static class ConfigurationHelper
   {
      public static string[] GetLabels(UserDefinedSettings settings)
      {
         if (!settings.CheckedLabelsFilter)
         {
             return null;
         }

         return settings.LastUsedLabels
            .Split(',')
            .Select(x => x.Trim(' '))
            .ToArray();
      }

#pragma warning disable 0649
      public struct HostInProjectsFile
      {
         public string Name;
         public IEnumerable<Project> Projects;
      }
#pragma warning restore 0649

      public static void SetupProjects(IEnumerable<HostInProjectsFile> projects, UserDefinedSettings settings)
      {
         if (projects == null)
         {
            return;
         }

         settings.SelectedProjects = projects
            .Where(x => x.Name != String.Empty && (x.Projects?.Count() ?? 0) > 0)
            .ToDictionary(
               item => item.Name,
               item => String.Join(",", item.Projects.Select(x => x.Path_With_Namespace + ":" + bool.TrueString)));
      }

      public static void SetProjectsForHost(string host, IEnumerable<Tuple<string, bool>> projects,
         UserDefinedSettings settings)
      {
         Dictionary<string, IEnumerable<Tuple<string, bool>>> allProjects = getAllProjects(settings);

         allProjects[host] = projects;

         settings.SelectedProjects = allProjects.ToDictionary(
            item => item.Key,
            item => String.Join(",", item.Value.Select(x => x.Item1.ToString() + ":" + x.Item2.ToString())));
      }

      public static IEnumerable<Tuple<string, bool>> GetProjectsForHost(string host, UserDefinedSettings settings)
      {
         if (String.IsNullOrEmpty(host) || !settings.SelectedProjects.ContainsKey(host))
         {
            return new Tuple<string, bool>[0];
         }

         string projectString = settings.SelectedProjects[host];
         return parseProjectString(projectString);
      }

      private static Dictionary<string, IEnumerable<Tuple<string, bool>>> getAllProjects(UserDefinedSettings settings)
      {
         return settings.SelectedProjects
            .ToDictionary(
               item => item.Key,
               item => parseProjectString(item.Value));
      }

      private static IEnumerable<Tuple<string, bool>> parseProjectString(string projectString)
      {
         if (projectString == String.Empty)
         {
            return new Tuple<string, bool>[0];
         }

         return projectString
            .Split(',')
            .Where(x => x.Split(':').Length == 2)
            .Select(x => parseProjectStringItem(x));
      }

      private static Tuple<string, bool> parseProjectStringItem(string item)
      {
         string[] splitted = item.Split(':');
         return new Tuple<string, bool>(
            splitted[0], bool.TryParse(splitted[1], out bool result) ? result : false);
      }
   }
}

