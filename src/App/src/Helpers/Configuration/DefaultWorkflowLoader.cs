using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.App.Helpers.GitLab;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;
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


      private static readonly string ProjectListFileName = "projects.json";

      internal static StringToBooleanCollection GetDefaultProjectsForHost(string hostname, bool enabledByDefault)
      {
         // Check if file exists. If it does not, it is not an error.
         string filepath = Path.Combine(Directory.GetCurrentDirectory(), ProjectListFileName);
         if (!System.IO.File.Exists(filepath))
         {
            return new StringToBooleanCollection();
         }

         IEnumerable<HostInProjectsFile> projectGroups;
         try
         {
            projectGroups = JsonUtils.LoadFromFile<IEnumerable<HostInProjectsFile>>(filepath);
         }
         catch (Exception ex) // Any exception from JsonUtils.LoadFromFile()
         {
            ExceptionHandlers.Handle("Cannot load projects from file", ex);
            return new StringToBooleanCollection();
         }

         foreach (HostInProjectsFile projectGroup in projectGroups)
         {
            if (StringUtils.GetHostWithPrefix(hostname) == StringUtils.GetHostWithPrefix(projectGroup.Name))
            {
               StringToBooleanCollection projects = new StringToBooleanCollection(projectGroup.Projects
                  .Select(project => new Tuple<string, bool>(project.Path_With_Namespace, enabledByDefault)));
               return projects;
            }
         }
         return new StringToBooleanCollection();
      }

      internal async static Task<StringToBooleanCollection> GetDefaultUsersForHost(
         GitLabInstance gitLabInstance, User currentUser)
      {
         UserAccessor userAccessor = new Shortcuts(gitLabInstance).GetUserAccessor();

         bool migratedLabels = false;
         StringToBooleanCollection labels = new StringToBooleanCollection();
         if (Program.Settings.DisplayFilterEnabled)
         {
            foreach (string keyword in GetDisplayFilterKeywords(Program.Settings))
            {
               string adjustedKeyword = keyword;
               if (keyword.StartsWith(Constants.GitLabLabelPrefix)
                || keyword.StartsWith(Constants.AuthorLabelPrefix))
               {
                  adjustedKeyword = keyword.Substring(1);
               }
               User user = await userAccessor.SearchUserByUsernameAsync(adjustedKeyword);
               if (user != null)
               {
                  if (!labels.Any(x => x.Item1 == user.Username))
                  {
                     labels.Add(new Tuple<string, bool>(user.Username, true));
                     migratedLabels |= true;
                  }
               }
            }
         }

         if (currentUser == null)
         {
            currentUser = await userAccessor.GetCurrentUserAsync();
         }
         if (currentUser != null)
         {
            if (!labels.Any(x => x.Item1 == currentUser.Username))
            {
               labels.Add(new Tuple<string, bool>(currentUser.Username, true));
            }
         }
         return labels;
      }
   }
}

