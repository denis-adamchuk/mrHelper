using System;
using System.Collections.Generic;
using System.Linq;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.Common
{
   static internal class GlobalCache
   {
      static internal User GetUser(string hostname, string username)
      {
         if (_users.TryGetValue(hostname, out HashSet<User> users))
         {
            return users.SingleOrDefault(x => 0 == String.Compare(x.Username, username, true));
         }
         return null;
      }

      static internal void AddUser(string hostname, User user)
      {
         if (!_users.ContainsKey(hostname))
         {
            _users[hostname] = new HashSet<User>();
         }
         _users[hostname].Add(user);
      }

      static internal ProjectKey? GetProjectKey(string hostname, int projectId)
      {
         if (_projects.TryGetValue(hostname, out Dictionary<int, ProjectKey> projects))
         {
            if (projects.TryGetValue(projectId, out ProjectKey projectKey))
            {
               return projectKey;
            }
         }
         return null;
      }

      static internal void AddProjectKey(string hostname, int projectId, ProjectKey projectKey)
      {
         if (!_projects.ContainsKey(hostname))
         {
            _projects[hostname] = new Dictionary<int, ProjectKey>();
         }
         _projects[hostname][projectId] = projectKey;
      }

      static Dictionary<string, HashSet<User>> _users =
         new Dictionary<string, HashSet<User>>();
      static Dictionary<string, Dictionary<int, ProjectKey>> _projects =
         new Dictionary<string, Dictionary<int, ProjectKey>>();
   }
}

