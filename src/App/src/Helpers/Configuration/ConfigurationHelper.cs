using GitLabSharp.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace mrHelper.App.Helpers
{
   public static class ConfigurationHelper
   {
      public static string[] GetDisplayFilterKeywords(UserDefinedSettings settings)
      {
         return settings.DisplayFilter
            .Split(',')
            .Select(x => x.Trim(' '))
            .ToArray();
      }

      // TODO
#pragma warning disable 0649
      public struct HostInProjectsFile
      {
         public HostInProjectsFile(string name, IEnumerable<Project> projects)
         {
            Name = name;
            Projects = projects;
         }

         public string Name { get; }
         public IEnumerable<Project> Projects { get; }
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

      public static void SetLabelsForHost(string host, IEnumerable<Tuple<string, bool>> labels,
         UserDefinedSettings settings)
      {
         settings.SelectedLabels = setItemsForHost(host, labels, settings.SelectedLabels);
      }

      public static void SetProjectsForHost(string host, IEnumerable<Tuple<string, bool>> projects,
         UserDefinedSettings settings)
      {
         settings.SelectedProjects = setItemsForHost(host, projects, settings.SelectedProjects);
      }

      public static IEnumerable<Tuple<string, bool>> GetLabelsForHost(string host, UserDefinedSettings settings)
      {
         return getItemsForHost(host, settings.SelectedLabels);
      }

      public static IEnumerable<Tuple<string, bool>> GetProjectsForHost(string host, UserDefinedSettings settings)
      {
         return getItemsForHost(host, settings.SelectedProjects);
      }

      public static IEnumerable<Project> GetEnabledProjects(string hostname, UserDefinedSettings settings)
      {
         if (String.IsNullOrEmpty(hostname))
         {
            return new Project[0];
         }

         IEnumerable<Tuple<string, bool>> projects = GetProjectsForHost(hostname, settings);
         Debug.Assert(projects != null);

         return projects
            .Where(x => x.Item2)
            .Select(x => x.Item1)
            .Select(x => new Project(x));
      }

      public static IEnumerable<string> GetEnabledLabels(string hostname, UserDefinedSettings settings)
      {
         if (String.IsNullOrEmpty(hostname))
         {
            return new string[0];
         }

         IEnumerable<Tuple<string, bool>> labels = GetLabelsForHost(hostname, settings);
         Debug.Assert(labels != null);

         return labels
            .Where(x => x.Item2)
            .Select(x => x.Item1);
      }

      private static Dictionary<string, string> setItemsForHost(string host,
         IEnumerable<Tuple<string, bool>> items, Dictionary<string, string> configuration)
      {
         Dictionary<string, IEnumerable<Tuple<string, bool>>> all = getAllItems(configuration);

         all[host] = items;

         return all.ToDictionary(
            item => item.Key,
            item => String.Join(",", item.Value.Select(x => x.Item1.ToString() + ":" + x.Item2.ToString())));
      }

      private static IEnumerable<Tuple<string, bool>> getItemsForHost(
         string host, Dictionary<string, string> configuration)
      {
         if (String.IsNullOrEmpty(host) || !configuration.ContainsKey(host))
         {
            return new Tuple<string, bool>[0];
         }

         string labelString = configuration[host];
         return parseItemWithStateString(labelString);
      }

      private static Dictionary<string, IEnumerable<Tuple<string, bool>>> getAllItems(
         Dictionary<string, string> allItems)
      {
         return allItems
            .ToDictionary(
               item => item.Key,
               item => parseItemWithStateString(item.Value));
      }

      private static IEnumerable<Tuple<string, bool>> parseItemWithStateString(string projectString)
      {
         if (projectString == String.Empty)
         {
            return new Tuple<string, bool>[0];
         }

         return projectString
            .Split(',')
            .Where(x => x.Split(':').Length == 2)
            .Select(x => parseItemWithStateStringElement(x));
      }

      private static Tuple<string, bool> parseItemWithStateStringElement(string element)
      {
         string[] splitted = element.Split(':');
         return new Tuple<string, bool>(
            splitted[0], bool.TryParse(splitted[1], out bool result) ? result : false);
      }
   }
}

