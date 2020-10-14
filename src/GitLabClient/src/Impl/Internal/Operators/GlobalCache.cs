using System;
using System.Collections.Generic;
using System.Linq;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;

namespace mrHelper.GitLabClient.Operators
{
   static internal class GlobalCache
   {
      static internal User GetAuthenticatedUser(string hostname, string accessToken)
      {
         return _authenticatedUsers.TryGetValue(
            new AuthenticatedUserKey(hostname, accessToken), out User user) ? user : null;
      }

      static internal void AddAuthenticatedUser(string hostname, string accessToken, User user)
      {
         _authenticatedUsers[new AuthenticatedUserKey(hostname, accessToken)] = user;
      }

      static internal User GetUser(string hostname, string username)
      {
         if (_usersByNames.TryGetValue(hostname, out HashSet<User> users))
         {
            return users.SingleOrDefault(x => x.Username == username);
         }
         return null;
      }

      static internal void AddUser(string hostname, User user)
      {
         if (!_usersByNames.ContainsKey(hostname))
         {
            _usersByNames[hostname] = new HashSet<User>();
         }
         _usersByNames[hostname].Add(user);
      }

      static internal ProjectKey? GetProjectKey(string hostname, int projectId)
      {
         if (_projectKeys.TryGetValue(hostname, out Dictionary<int, ProjectKey> projects))
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
         if (!_projectKeys.ContainsKey(hostname))
         {
            _projectKeys[hostname] = new Dictionary<int, ProjectKey>();
         }
         _projectKeys[hostname][projectId] = projectKey;
      }

      static internal IEnumerable<Project> GetProjects(string hostname)
      {
         return _projects.TryGetValue(hostname, out IEnumerable<Project> projects) ? projects : null;
      }

      static internal void SetProjects(string hostname, IEnumerable<Project> projects)
      {
         _projects[hostname] = projects.ToArray();
      }

      static internal IEnumerable<User> GetUsers(string hostname)
      {
         return _users.TryGetValue(hostname, out IEnumerable<User> users) ? users : null;
      }

      static internal void SetUsers(string hostname, IEnumerable<User> users)
      {
         _users[hostname] = users.ToArray();
      }

      private struct AuthenticatedUserKey : IEquatable<AuthenticatedUserKey>
      {
         internal AuthenticatedUserKey(string hostName, string accessToken)
         {
            HostName = hostName;
            AccessToken = accessToken;
         }

         public override bool Equals(object obj)
         {
            return obj is AuthenticatedUserKey key && Equals(key);
         }

         public bool Equals(AuthenticatedUserKey other)
         {
            return HostName == other.HostName &&
                   AccessToken == other.AccessToken;
         }

         public override int GetHashCode()
         {
            int hashCode = -1402912620;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(HostName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(AccessToken);
            return hashCode;
         }

         internal string HostName;
         internal string AccessToken;
      }

      static private readonly Dictionary<AuthenticatedUserKey, User> _authenticatedUsers =
         new Dictionary<AuthenticatedUserKey, User>();
      static private readonly Dictionary<string, HashSet<User>> _usersByNames =
         new Dictionary<string, HashSet<User>>();
      static private readonly Dictionary<string, Dictionary<int, ProjectKey>> _projectKeys =
         new Dictionary<string, Dictionary<int, ProjectKey>>();
      static private readonly Dictionary<string, IEnumerable<Project>> _projects =
         new Dictionary<string, IEnumerable<Project>>();
      static private readonly Dictionary<string, IEnumerable<User>> _users =
         new Dictionary<string, IEnumerable<User>>();
   }
}

