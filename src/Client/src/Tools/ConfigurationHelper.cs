using GitLabSharp.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace mrHelper.Client.Tools
{
   public static class ConfigurationHelper
   {
      public static string GetAccessToken(string hostname, UserDefinedSettings settings)
      {
         for (int iKnownHost = 0; iKnownHost < settings.KnownHosts.Count; ++iKnownHost)
         {
            if (hostname == settings.KnownHosts[iKnownHost])
            {
               return settings.KnownAccessTokens[iKnownHost];
            }
         }
         return String.Empty;
      }

      public static string[] GetLabels(UserDefinedSettings settings)
      {
         if (!settings.CheckedLabelsFilter)
         {
             return null;
         }

         return settings.LastUsedLabels .Split(',').Select(x => x.Trim(' ')).ToArray();
      }

#pragma warning disable 0649
      public struct HostInProjectsFile
      {
         public string Name;
         public List<Project> Projects;
      }
#pragma warning restore 0649

      public static void SetProjects(List<HostInProjectsFile> projects, UserDefinedSettings settings)
      {
         if (projects == null)
         {
            return;
         }

         settings.SelectedProjects = projects
            .Where(x => x.Name != String.Empty && (x.Projects?.Count ?? 0) > 0)
            .ToDictionary(
               item => item.Name, item => String.Join(",", item.Projects.Select(x => x.Path_With_Namespace)));
      }

      public static string[] GetProjectsForHost(string host, UserDefinedSettings settings)
      {
         if (!settings.SelectedProjects.ContainsKey(host))
         {
            return null;
         }

         string projects = settings.SelectedProjects[host];
         if (projects == String.Empty)
         {
            return null;
         }

         return projects.Split(',');
      }
   }
}

