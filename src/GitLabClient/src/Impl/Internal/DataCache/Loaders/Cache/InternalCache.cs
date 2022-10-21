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
         init();
      }

      private InternalCache(InternalCache details)
      {
         init();

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

         foreach (KeyValuePair<MergeRequestKey, MergeRequestApprovalConfiguration> kv in details._approvals)
         {
            SetApprovals(kv.Key, kv.Value);
         }
      }

      private void init()
      {
         _mergeRequests = new Dictionary<ProjectKey, IEnumerable<MergeRequest>>();
         _versions = new Dictionary<MergeRequestKey, IEnumerable<Version>>();
         _commits = new Dictionary<MergeRequestKey, IEnumerable<Commit>>();
         _approvals = new Dictionary<MergeRequestKey, MergeRequestApprovalConfiguration>();
         _avatars = new Dictionary<int, byte[]>();
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
      /// Return single merge request by its key
      /// </summary>
      public MergeRequest GetMergeRequest(MergeRequestKey mrk)
      {
         if (!_mergeRequests.TryGetValue(mrk.ProjectKey, out var mergeRequests))
         {
            return null;
         }

         return mergeRequests.SingleOrDefault(mergeRequest => mergeRequest.IId == mrk.IId);
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
      /// Return all cached approvals
      /// </summary>
      public MergeRequestApprovalConfiguration GetApprovals(MergeRequestKey mrk)
      {
         return _approvals.ContainsKey(mrk) ? _approvals[mrk] : default(MergeRequestApprovalConfiguration);
      }

      /// <summary>
      /// Update cached approvals
      /// </summary>
      internal void SetApprovals(MergeRequestKey mrk, MergeRequestApprovalConfiguration approvals)
      {
         _approvals[mrk] = approvals;
      }

      public byte[] GetAvatar(int userId)
      {
         return _avatars.TryGetValue(userId, out byte[] value) ? value : null;
      }

      internal void SetAvatar(int userId, byte[] avatar)
      {
         _avatars[userId] = avatar;
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
         else
         {
            _mergeRequests[mrk.ProjectKey] = new MergeRequest[] { mergeRequest };
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

      // maps unique project id to list of merge requests
      private Dictionary<ProjectKey, IEnumerable<MergeRequest>> _mergeRequests;

      // maps Merge Request to its versions
      private Dictionary<MergeRequestKey, IEnumerable<Version>> _versions;

      // maps Merge Request to its commits
      private Dictionary<MergeRequestKey, IEnumerable<Commit>> _commits;

      // maps Merge Request to its approval configuration
      private Dictionary<MergeRequestKey, MergeRequestApprovalConfiguration> _approvals;

      // maps User Id to its avatar
      private Dictionary<int, byte[]> _avatars;
   }
}

