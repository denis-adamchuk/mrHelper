using System;
using System.Linq;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using Version = GitLabSharp.Entities.Version;

namespace mrHelper.GitLabClient.Loaders.Cache
{
   internal class InternalCache : IInternalCache
   {
      internal InternalCache()
      {
         _mergeRequests = new Dictionary<ProjectKey, IEnumerable<MergeRequest>>();
         _versions = new Dictionary<MergeRequestKey, IEnumerable<Version>>();
         _commits = new Dictionary<MergeRequestKey, IEnumerable<Commit>>();
      }

      private InternalCache(InternalCache details)
      {
         _mergeRequests = new Dictionary<ProjectKey, IEnumerable<MergeRequest>>();
         _versions = new Dictionary<MergeRequestKey, IEnumerable<Version>>();
         _commits = new Dictionary<MergeRequestKey, IEnumerable<Commit>>();

         foreach (KeyValuePair<ProjectKey, IEnumerable<MergeRequest>> kv in details._mergeRequests)
         {
            _mergeRequests[kv.Key] = kv.Value.ToArray(); // make a copy
         }

         foreach (KeyValuePair<MergeRequestKey, IEnumerable<Version>> kv in details._versions)
         {
            SetVersions(kv.Key, kv.Value.ToArray()); // make a copy
         }

         foreach (KeyValuePair<MergeRequestKey, IEnumerable<Commit>> kv in details._commits)
         {
            SetCommits(kv.Key, kv.Value.ToArray()); // make a copy
         }
      }

      /// <summary>
      /// Create a copy of object
      /// </summary>
      public IInternalCache Clone()
      {
         return new InternalCache(this);
      }

      /// <summary>
      /// Return a list of cached projects
      /// </summary>
      public IEnumerable<ProjectKey> GetProjects()
      {
         return _mergeRequests.Keys;
      }

      /// <summary>
      /// Return a list of merge requests by unique project id
      /// </summary>
      public IEnumerable<MergeRequest> GetMergeRequests(ProjectKey key)
      {
         return _mergeRequests.ContainsKey(key) ? _mergeRequests[key] : Array.Empty<MergeRequest>();
      }

      /// <summary>
      /// Sets a list of merge requests for the given projects
      /// </summary>
      internal void SetMergeRequests(Dictionary<ProjectKey, IEnumerable<MergeRequest>> mergeRequests)
      {
         _mergeRequests = mergeRequests;
      }

      /// <summary>
      /// Return a timestamp of the most recent version of a specified merge request
      /// </summary>
      public IEnumerable<Version> GetVersions(MergeRequestKey mrk)
      {
         return _versions.ContainsKey(mrk) ? _versions[mrk] : Array.Empty<Version>();
      }

      /// <summary>
      /// Update a timestamp of the most recent version of a specified merge request
      /// </summary>
      internal void SetVersions(MergeRequestKey mrk, IEnumerable<Version> versions)
      {
         _versions[mrk] = versions;
      }

      /// <summary>
      /// Return all cached commits
      /// </summary>
      public IEnumerable<Commit> GetCommits(MergeRequestKey mrk)
      {
         return _commits.ContainsKey(mrk) ? _commits[mrk] : Array.Empty<Commit>();
      }

      /// <summary>
      /// Update cached commits
      /// </summary>
      internal void SetCommits(MergeRequestKey mrk, IEnumerable<Commit> commits)
      {
         _commits[mrk] = commits;
      }

      /// <summary>
      /// Updates a merge request
      /// </summary>
      internal void UpdateMergeRequest(MergeRequestKey mrk, MergeRequest mergeRequest)
      {
         if (_mergeRequests.ContainsKey(mrk.ProjectKey))
         {
            List<MergeRequest> mergeRequests = _mergeRequests[mrk.ProjectKey].ToList(); // make a copy
            int index = mergeRequests.FindIndex(x => x.IId == mrk.IId);
            if (index != -1)
            {
               mergeRequests[index] = mergeRequest; // substitute an item
            }
            else
            {
               mergeRequests.Add(mergeRequest); // add an item
            }
            _mergeRequests[mrk.ProjectKey] = mergeRequests;
         }
      }

      /// <summary>
      /// Remove records from Changes collection
      /// </summary>
      internal void CleanupVersions(MergeRequestKey mrk)
      {
         _versions.Remove(mrk);
         _commits.Remove(mrk);
      }

      public IEnumerable<Project> GetAllProjects()
      {
         return _projects;
      }

      internal void SetAllProjects(IEnumerable<Project> projects)
      {
         _projects = projects.ToArray();
      }

      public IEnumerable<User> GetAllUsers()
      {
         return _users;
      }

      internal void SetAllUsers(IEnumerable<User> users)
      {
         _users = users.ToArray();
      }

      // collection of all projects
      private Project[] _projects;

      // collection of all users
      private User[] _users;

      // maps unique project id to list of merge requests
      private Dictionary<ProjectKey, IEnumerable<MergeRequest>> _mergeRequests;

      // maps Merge Request to its versions
      private readonly Dictionary<MergeRequestKey, IEnumerable<Version>> _versions;

      // maps Merge Request to its commits
      private readonly Dictionary<MergeRequestKey, IEnumerable<Commit>> _commits;
   }
}

