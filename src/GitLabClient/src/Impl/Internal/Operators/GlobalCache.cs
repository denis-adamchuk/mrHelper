using System;
using System.Collections.Generic;
using System.Linq;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using Version = GitLabSharp.Entities.Version;

namespace mrHelper.GitLabClient.Operators
{
   public struct CachedDiscussionsTimestamp
   {
      public CachedDiscussionsTimestamp(DateTime time, int count)
      {
         Time = time;
         Count = count;
      }

      public DateTime Time { get; }
      public int Count { get; }
   }

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

      static internal Commit GetCommit(ProjectKey projectKey, string id)
      {
         foreach (KeyValuePair<MergeRequestKey,
            TimedEntity<WeakReference<IEnumerable<Commit>>, string>> commitCollection in _commits)
         {
            if (commitCollection.Key.ProjectKey.Equals(projectKey))
            {
               if (commitCollection.Value.Data.TryGetTarget(out IEnumerable<Commit> target))
               {
                  Commit matchingCommit = target.SingleOrDefault(commit => commit.Id == id);
                  if (matchingCommit != null)
                  {
                     return matchingCommit;
                  }
               }
            }
         }
         return null;
      }

      static internal IEnumerable<Version> GetVersions(MergeRequestKey mrk, string cachedRevisionTimestamp)
      {
         return getCachedVersions(mrk, cachedRevisionTimestamp);
      }

      static internal void SetVersions(MergeRequestKey mrk, IEnumerable<Version> versions,
         string revisionTimestamp)
      {
         WeakReference<IEnumerable<Version>> weakVersions = new WeakReference<IEnumerable<Version>>(versions);
         _versions[mrk] = new TimedEntity<WeakReference<IEnumerable<Version>>, string>(weakVersions, revisionTimestamp);
      }

      static internal IEnumerable<Commit> GetCommits(MergeRequestKey mrk, string cachedRevisionTimestamp)
      {
         return getCachedCommits(mrk, cachedRevisionTimestamp);
      }

      static internal void SetCommits(MergeRequestKey mrk, IEnumerable<Commit> commits,
         string revisionTimestamp)
      {
         WeakReference<IEnumerable<Commit>> weakCommits = new WeakReference<IEnumerable<Commit>>(commits);
         _commits[mrk] = new TimedEntity<WeakReference<IEnumerable<Commit>>, string>(weakCommits, revisionTimestamp);
      }

      static internal IEnumerable<Discussion> GetDiscussions(MergeRequestKey mrk,
         CachedDiscussionsTimestamp cachedRevisionTimestamp)
      {
         return getCachedDiscussions(mrk, cachedRevisionTimestamp);
      }

      static internal void SetDiscussions(MergeRequestKey mrk, IEnumerable<Discussion> discussions,
         CachedDiscussionsTimestamp revisionTimestamp)
      {
         WeakReference<IEnumerable<Discussion>> weakDiscussions = new WeakReference<IEnumerable<Discussion>>(discussions);
         _discussions[mrk] = new TimedEntity<WeakReference<IEnumerable<Discussion>>, CachedDiscussionsTimestamp>(
            weakDiscussions, revisionTimestamp);
      }

      // TODO See comment in DiscussionManager
      static internal void DeleteDiscussions(MergeRequestKey mrk)
      {
         _discussions.Remove(mrk);
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

         internal string HostName { get; }
         internal string AccessToken { get; }
      }

      private static IEnumerable<Version> getCachedVersions(MergeRequestKey mrk, string cachedRevisionTimestamp)
      {
         if (!_versions.TryGetValue(mrk, out var versionCollection))
         {
            return null;
         }

         if (!versionCollection.Data.TryGetTarget(out var target))
         {
            return null;
         }

         if (versionCollection.Timestamp != cachedRevisionTimestamp)
         {
            return null;
         }

         return target;
      }

      private static IEnumerable<Commit> getCachedCommits(MergeRequestKey mrk, string cachedRevisionTimestamp)
      {
         if (!_commits.TryGetValue(mrk, out var commitCollection))
         {
            return null;
         }

         if (!commitCollection.Data.TryGetTarget(out var target))
         {
            return null;
         }

         if (commitCollection.Timestamp != cachedRevisionTimestamp)
         {
            return null;
         }

         return target;
      }

      private static IEnumerable<Discussion> getCachedDiscussions(MergeRequestKey mrk,
         CachedDiscussionsTimestamp cachedRevisionTimestamp)
      {
         if (!_discussions.TryGetValue(mrk, out var discussionCollection))
         {
            return null;
         }

         if (!discussionCollection.Data.TryGetTarget(out var target))
         {
            return null;
         }

         if (discussionCollection.Timestamp.Time != cachedRevisionTimestamp.Time)
         {
            return null;
         }

         if (discussionCollection.Timestamp.Count != cachedRevisionTimestamp.Count)
         {
            return null;
         }

         return target;
      }

      private struct TimedEntity<TData, TTimestamp>
      {
         public TimedEntity(TData data, TTimestamp timestamp)
         {
            Data = data;
            Timestamp = timestamp;
         }

         internal TData Data { get; }
         internal TTimestamp Timestamp { get; }

         public static implicit operator TimedEntity<TData, TTimestamp>(TimedEntity<IEnumerable<Version>, DateTime> v)
         {
            throw new NotImplementedException();
         }
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
      static private readonly Dictionary<MergeRequestKey,
         TimedEntity<WeakReference<IEnumerable<Version>>, string>> _versions =
            new Dictionary<MergeRequestKey, TimedEntity<WeakReference<IEnumerable<Version>>, string>>();
      static private readonly Dictionary<MergeRequestKey,
         TimedEntity<WeakReference<IEnumerable<Commit>>, string>> _commits =
            new Dictionary<MergeRequestKey, TimedEntity<WeakReference<IEnumerable<Commit>>, string>>();
      static private readonly Dictionary<MergeRequestKey,
         TimedEntity<WeakReference<IEnumerable<Discussion>>, CachedDiscussionsTimestamp>> _discussions =
            new Dictionary<MergeRequestKey, TimedEntity<WeakReference<IEnumerable<Discussion>>, CachedDiscussionsTimestamp>>();
   }
}

