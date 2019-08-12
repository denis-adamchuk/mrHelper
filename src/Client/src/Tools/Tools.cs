using System;
using System.Collections.Generic;
using System.Configuration;
using System.ComponentModel;

namespace mrHelper.Client
{
   public static class Tools
   {
      private static const string ProjectListFileName = "projects.json";
      private const string CustomActionsFileName = "CustomActions.xml";

      public static List<ICommand> LoadCustomActions()
      {
         CustomCommandLoader loader = new CustomCommandLoader(this);
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

      public static List<Project> LoadProjectsFromFile()
      {
         // Check if file exists. If it does not, it is not an error.
         if (File.Exists(ProjectListFileName))
         {
            try
            {
               return loadProjectsFromFile(GetCurrentHostName(), ProjectListFileName);
            }
            catch (Exception ex) // whatever de-serialization exception
            {
               ExceptionHandlers.Handle(ex, "Cannot load projects from file");
            }
         }
         return null;
      }

      public static string GetAccessToken(string hostname, UserDefinedSettings settings)
      {
         for (int iKnownHost = 0; iKnownHost < settings.KnownHosts.Count; ++iKnownHost)
         {
            if (host == settings.KnownHosts[iKnownHost])
            {
               return settings.KnownAccessTokens[iKnownHost];
            }
         }
         return String.Empty;
      }

      public static List<string> SplitLabels(string labels)
      {
         var result = new List<string>();
         foreach (var item in labels.Split(','))
         {
            result.Add(item.Trim(' '));
         }
         return result;
      }
   }
}

