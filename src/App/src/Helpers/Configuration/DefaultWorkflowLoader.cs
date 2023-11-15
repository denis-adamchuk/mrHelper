using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.App.Helpers.GitLab;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;
using mrHelper.GitLabClient;
using Newtonsoft.Json;
using static mrHelper.App.Helpers.ConfigurationHelper;

namespace mrHelper.App.Helpers
{
   internal static class DefaultWorkflowLoader
   {
      public class HostInProjectsFile
      {
         [JsonProperty]
         public string Name { get; protected set; }

         [JsonProperty]
         public IEnumerable<Project> Projects { get; protected set; }

         [JsonProperty]
         public IEnumerable<Project> Projects_With_Env { get; protected set; }
      }


      private static readonly string ProjectListFileName = "projects.json";

      internal static StringToBooleanCollection GetDefaultProjectsForHost(string hostname, bool enabledByDefault)
      {
         IEnumerable<HostInProjectsFile> projectGroups = load();
         if (projectGroups != null)
         {
            foreach (HostInProjectsFile projectGroup in projectGroups)
            {
               if (StringUtils.GetHostWithPrefix(hostname) == StringUtils.GetHostWithPrefix(projectGroup.Name))
               {
                  if (projectGroup.Projects == null)
                  {
                     break;
                  }
                  return new StringToBooleanCollection(projectGroup.Projects
                     .Select(project => new Tuple<string, bool>(project.Path_With_Namespace, enabledByDefault)));
               }
            }
         }
         return new StringToBooleanCollection();
      }

      internal static StringToBooleanCollection GetDefaultProjectsWithEnvironments(string hostname, bool enabledByDefault)
      {
         IEnumerable<HostInProjectsFile> projectGroups = load();
         if (projectGroups != null)
         {
            foreach (HostInProjectsFile projectGroup in projectGroups)
            {
               if (StringUtils.GetHostWithPrefix(hostname) == StringUtils.GetHostWithPrefix(projectGroup.Name))
               {
                  if (projectGroup.Projects_With_Env == null)
                  {
                     break;
                  }
                  return new StringToBooleanCollection(projectGroup.Projects_With_Env
                     .Select(project => new Tuple<string, bool>(project.Path_With_Namespace, enabledByDefault)));
               }
            }
         }
         return new StringToBooleanCollection();
      }

      internal async static Task<string> GetDefaultUserForHost(
         GitLabInstance gitLabInstance, User currentUser)
      {
         UserAccessor userAccessor = new Shortcuts(gitLabInstance).GetUserAccessor();

         if (currentUser == null)
         {
            currentUser = await userAccessor.GetCurrentUserAsync();
         }
         return currentUser?.Username;
      }

      private static IEnumerable<HostInProjectsFile> load()
      {
         // Check if file exists. If it does not, it is not an error.
         string filepath = Path.Combine(Directory.GetCurrentDirectory(), ProjectListFileName);
         if (!System.IO.File.Exists(filepath))
         {
            return null;
         }

         IEnumerable<HostInProjectsFile> projectGroups;
         try
         {
            projectGroups = JsonUtils.LoadFromFile<IEnumerable<HostInProjectsFile>>(filepath);
         }
         catch (Exception ex) // Any exception from JsonUtils.LoadFromFile()
         {
            ExceptionHandlers.Handle("Cannot load projects from file", ex);
            return null;
         }

         return projectGroups;
      }
   }
}

