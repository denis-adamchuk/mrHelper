using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using GitLabSharp.Entities;
using mrHelper.Common.Constants;
using mrHelper.Common.Tools;
using mrHelper.Common.Interfaces;

namespace mrHelper.GitLabClient
{
   public static class Helpers
   {
      public static bool IsUserMentioned(string text, User user)
      {
         if (user == null)
         {
            Debug.Assert(false);
            return false;
         }

         if (StringUtils.ContainsNoCase(text, user.Name))
         {
            return true;
         }

         string label = Constants.GitLabLabelPrefix + user.Username;
         int idx = text.IndexOf(label, StringComparison.CurrentCultureIgnoreCase);
         while (idx >= 0)
         {
            if (idx == text.Length - label.Length)
            {
               // text ends with label
               return true;
            }

            if (!Char.IsLetter(text[idx + label.Length]))
            {
               // label is in the middle of text
               return true;
            }

            Debug.Assert(idx != text.Length - 1);
            idx = text.IndexOf(label, idx + 1, StringComparison.CurrentCultureIgnoreCase);
         }

         return false;
      }

      public struct ProjectBranchKey : IEquatable<ProjectBranchKey>
      {
         public ProjectBranchKey(string projectName, string branchName) : this()
         {
            ProjectName = projectName;
            BranchName = branchName;
         }

         public string ProjectName { get; }
         public string BranchName { get; }

         public override bool Equals(object obj)
         {
            return obj is ProjectBranchKey key && Equals(key);
         }

         public bool Equals(ProjectBranchKey other)
         {
            return ProjectName == other.ProjectName &&
                   BranchName == other.BranchName;
         }

         public override int GetHashCode()
         {
            var hashCode = -872655413;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ProjectName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(BranchName);
            return hashCode;
         }
      }

      public static IEnumerable<ProjectBranchKey> GetSourceBranchesByUser(User user, DataCache dataCache)
      {
         if (dataCache == null || dataCache.MergeRequestCache == null)
         {
            Debug.Assert(false);
            return Array.Empty<ProjectBranchKey>();
         }

         List<ProjectBranchKey> result = new List<ProjectBranchKey>();
         foreach (ProjectKey projectKey in dataCache.MergeRequestCache.GetProjects())
         {
            IEnumerable<MergeRequest> mergeRequestsByUser = dataCache.MergeRequestCache.GetMergeRequests(projectKey)
               .Where(mergeRequest => mergeRequest.Author.Id == user.Id);
            result.AddRange(mergeRequestsByUser
               .Select(mergeRequest => new ProjectBranchKey(projectKey.ProjectName, mergeRequest.Source_Branch)));
         }
         return result;
      }

      // Checks if a merge request matches ANY of search queries inside `queries`
      public static bool DoesMatchSearchQuery(SearchQueryCollection queries, MergeRequest mergeRequest,
         ProjectKey projectKey)
      {
         // TODO Test each condition
         foreach (SearchQuery query in queries.Queries)
         {
            if ((query.State == null || query.State == mergeRequest.State)
                && (query.IId.HasValue && query.IId.Value == mergeRequest.IId
                ||  query.ProjectName == projectKey.ProjectName
                ||  query.TargetBranchName == mergeRequest.Target_Branch
                || (query.Text != null && mergeRequest.Title.ToLower().Contains(query.Text.ToLower()))
                || (query.Text != null && mergeRequest.Description.ToLower().Contains(query.Text.ToLower()))
                || (query.Labels != null && query.Labels.All(label => mergeRequest.Labels.Contains(label)))
                || (query.AuthorUserName != null && mergeRequest.Author.Username == query.AuthorUserName)))
            {
               return true;
            }
         }
         return false;
      }
   }
}

