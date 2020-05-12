using System;
using System.Linq;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Client.Types;
using mrHelper.Common.Interfaces;
using Version = GitLabSharp.Entities.Version;

namespace mrHelper.Client.MergeRequests
{
   internal class InternalCache : IInternalCache
   {
      internal InternalCache()
      {
         _mergeRequests = new Dictionary<ProjectKey, MergeRequest[]>();
         _changes = new Dictionary<MergeRequestKey, Version[]>();
         _commits = new Dictionary<MergeRequestKey, Commit[]>();
      }

      private InternalCache(InternalCache details)
      {
         _mergeRequests = details._mergeRequests.ToDictionary(
            item => item.Key,
            item => item.Value.ToArray());

         _changes = details._changes.ToDictionary(
            item => item.Key,
            item => item.Value.ToArray());

         _commits = details._commits.ToDictionary(
            item => item.Key,
            item => item.Value.ToArray());
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
         return _mergeRequests.ContainsKey(key) ?
            _mergeRequests[key].ToArray() : Array.Empty<MergeRequest>();
      }

      /// <summary>
      /// Sets a list of merge requests for the given project
      /// </summary>
      internal void SetMergeRequests(ProjectKey key, IEnumerable<MergeRequest> mergeRequests)
      {
         _mergeRequests[key] = mergeRequests.ToArray();
      }

      /// <summary>
      /// Return a timestamp of the most recent version of a specified merge request
      /// </summary>
      public IEnumerable<Version> GetVersions(MergeRequestKey mrk)
      {
         return _changes.ContainsKey(mrk) ? _changes[mrk] : Array.Empty<Version>();
      }

      /// <summary>
      /// Update a timestamp of the most recent version of a specified merge request
      /// </summary>
      internal void SetVersions(MergeRequestKey mrk, IEnumerable<Version> versions)
      {
         _changes[mrk] = versions.ToArray();
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
         _commits[mrk] = commits.ToArray();
      }

      /// <summary>
      /// Updates a merge request
      /// </summary>
      internal void UpdateMergeRequest(MergeRequestKey mrk, MergeRequest mergeRequest)
      {
         if (_mergeRequests.ContainsKey(mrk.ProjectKey))
         {
            int index = Array.FindIndex(_mergeRequests[mrk.ProjectKey], x => x.IId == mrk.IId);
            if (index != -1)
            {
               _mergeRequests[mrk.ProjectKey][index] = mergeRequest;
            }
         }
      }

      /// <summary>
      /// Remove records from Changes collection
      /// </summary>
      internal void CleanupVersions(MergeRequestKey mrk)
      {
         _changes.Remove(mrk);
         _commits.Remove(mrk);
      }

      // maps unique project id to list of merge requests
      private readonly Dictionary<ProjectKey, MergeRequest[]> _mergeRequests;

      // maps Merge Request to a timestamp of its latest version
      private readonly Dictionary<MergeRequestKey, Version[]> _changes;

      // maps Merge Request to a timestamp of its latest version
      private readonly Dictionary<MergeRequestKey, Commit[]> _commits;
   }
}

