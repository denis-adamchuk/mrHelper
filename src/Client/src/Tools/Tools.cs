using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Configuration;
using System.ComponentModel;
using System.Web.Script.Serialization;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;
using mrHelper.CustomActions;

namespace mrHelper.Client.Tools
{
   public static class Tools
   {
      public const string ProjectListFileName = "projects.json";
      private const string CustomActionsFileName = "CustomActions.xml";

      public static List<ICommand> LoadCustomActions(ICommandCallback callback)
      {
         CustomCommandLoader loader = new CustomCommandLoader(callback);
         try
         {
            return loader.LoadCommands(CustomActionsFileName);
         }
         catch (CustomCommandLoaderException ex)
         {
            // If file doesn't exist the loader throws, leaving the app in an undesirable state.
            // Do not try to load custom actions if they don't exist.
            ExceptionHandlers.Handle(ex, "Cannot load custom actions");
         }
         return null;
      }

      public static List<Project> LoadProjectsFromFile(string hostname)
      {
         // Check if file exists. If it does not, it is not an error.
         if (System.IO.File.Exists(ProjectListFileName))
         {
            try
            {
               return loadProjectsFromFile(hostname, ProjectListFileName);
            }
            catch (Exception ex) // whatever de-serialization exception
            {
               ExceptionHandlers.Handle(ex, "Cannot load projects from file");
            }
         }
         return null;
      }

      public static string UnknownHostToken = String.Empty;

      public static string GetAccessToken(string hostname, UserDefinedSettings settings)
      {
         for (int iKnownHost = 0; iKnownHost < settings.KnownHosts.Count; ++iKnownHost)
         {
            if (hostname == settings.KnownHosts[iKnownHost])
            {
               return settings.KnownAccessTokens[iKnownHost];
            }
         }
         return UnknownHostToken;
      }

      public static List<MergeRequest> FilterMergeRequests(List<MergeRequest> mergeRequests,
         UserDefinedSettings settings)
      {
         Func<MergeRequest, bool> DoesMatchLabels =
            (x) =>
         {
            Func<string, List<string>> SplitLabels =
               (labels) =>
            {
               List<string> result = new List<string>();
               foreach (var item in labels.Split(','))
               {
                  result.Add(item.Trim(' '));
               }
               return result;
            };

            if (!settings.CheckedLabelsFilter)
            {
               return true;
            }
            return SplitLabels(settings.LastUsedLabels).Intersect(x.Labels).Count() != 0;
         };

         return mergeRequests.Where((x) => DoesMatchLabels(x)).ToList();
      }

      private class HostInProjectsFile
      {
         public string Name = null;
         public List<Project> Projects = null;
      }

      /// <summary>
      /// Loads project list from file with JSON format
      /// Throws ArgumentException
      /// Throws ArgumentNullException
      /// Throws InvalidOperationException
      /// </summary>
      static private List<Project> loadProjectsFromFile(string hostname, string filename)
      {
         Debug.Assert(System.IO.File.Exists(filename));

         string json = System.IO.File.ReadAllText(filename);

         JavaScriptSerializer serializer = new JavaScriptSerializer();

         List<HostInProjectsFile> hosts;
         try
         {
            hosts = serializer.Deserialize<List<HostInProjectsFile>>(json);
         }
         catch (Exception) // whatever de-serialization exception
         {
            throw;
         }

         foreach (var host in hosts)
         {
            if (host.Name == hostname)
            {
               return host.Projects;
            }
         }

         return null;
      }
   }
}

