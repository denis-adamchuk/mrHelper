﻿using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using GitLabSharp.Entities;
using mrHelper.Common.Constants;
using mrHelper.Common.Tools;
using mrHelper.Common.Interfaces;

namespace mrHelper.GitLabClient
{
   public static class Helpers
   {
      private static readonly Regex GitLabVersionRegex = new Regex(
         @"(?'major_version'\d*)\.(?'minor_version'\d*)\.", RegexOptions.Compiled | RegexOptions.IgnoreCase);
      private static readonly System.Version EarliestGitLabVersionWithApprovalsSupport = new System.Version(13, 6);

      /// <summary>
      /// This is a VERY simplified way of functionality checking because GitLab has complicated editions and plans.
      /// TODO Make approval status support check better.
      /// </summary>
      public static bool DoesGitLabVersionSupportApprovals(GitLabVersion version)
      {
         Debug.Assert(version != null);

         Match m = GitLabVersionRegex.Match(version.Version);
         if (m.Success
          && m.Groups["major_version"].Success
          && int.TryParse(m.Groups["major_version"].Value, out int major_version)
          && m.Groups["minor_version"].Success
          && int.TryParse(m.Groups["minor_version"].Value, out int minor_version))
         {
            System.Version gitLabVersion = new System.Version(major_version, minor_version);
            return gitLabVersion >= EarliestGitLabVersionWithApprovalsSupport;
         }
         return false;
      }

      public static string GetCreateAccessTokenUrl(string hostname)
      {
         if (String.IsNullOrEmpty(hostname))
         {
            return null;
         }

         string url_suffix = @"/-/profile/personal_access_tokens/";
         return StringUtils.GetHostWithPrefix(hostname + url_suffix);
      }

      public static string GetVersionLoaderKey(MergeRequest mergeRequest)
      {
         return mergeRequest == null ? null : mergeRequest.Sha + mergeRequest.Target_Branch;
      }

      public static bool CheckConditions(IEnumerable<string> conditions,
         IEnumerable<User> approvedBy, IEnumerable<string> labels, User author,
         bool isExcluded, bool isTrackingTime, bool isPinned)
      {
         return conditions.All(cond => checkCondition(cond, approvedBy, labels, author,
            isExcluded, isTrackingTime, isPinned));
      }

      private static bool checkCondition(string condition,
         IEnumerable<User> approvedBy, IEnumerable<string> labels, User author,
         bool isExcluded, bool isTrackingTime, bool isPinned)
      {
         if (String.IsNullOrEmpty(condition))
         {
            return true;
         }

         string excludePrefix = "NOT ";
         bool isExpected = !condition.StartsWith(excludePrefix);
         condition = isExpected ? condition : condition.Substring(excludePrefix.Length);

         string[] splitted = condition.TrimStart('{').TrimEnd('}').Split(':');
         if (splitted.Length != 2)
         {
            return false;
         }

         string conditionName = splitted[0];
         string conditionValue = splitted[1];
         if (conditionName == "Label")
         {
            return isExpected == labels.Any(label => String.Compare(conditionValue, label, true) == 0);
         }
         else if (conditionName == "LabelSubString")
         {
            return isExpected == labels.Any(label => StringUtils.ContainsNoCase(label, conditionValue));
         }
         else if (conditionName == "Author")
         {
            return isExpected == (String.Compare(conditionValue, author.Username, true) == 0);
         }
         else if (conditionName == "Approved_By")
         {
            return isExpected == approvedBy.Any(approve => String.Compare(conditionValue, approve.Username, true) == 0);
         }
         else if (conditionName == "IsExcluded")
         {
            return isExpected == (String.Compare(conditionValue, isExcluded.ToString(), true) == 0);
         }
         else if (conditionName == "IsTrackingTime")
         {
            return isExpected == (String.Compare(conditionValue, isTrackingTime.ToString(), true) == 0);
         }
         else if (conditionName == "IsPinned")
         {
            return isExpected == (String.Compare(conditionValue, isPinned.ToString(), true) == 0);
         }

         Debug.Assert(false);
         return false;
      }

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

      private struct LabelGroup
      {
         internal IEnumerable<string> Labels;
         internal int Priority;
      }

      public static IEnumerable<string> GroupLabels(IEnumerable<FullMergeRequestKey> keys,
         IEnumerable<string> unimportantSuffices, User currentUser)
      {
         int getPriority(IEnumerable<string> labels)
         {
            Debug.Assert(labels.Any());
            if (GitLabClient.Helpers.IsUserMentioned(labels.First(), currentUser))
            {
               return 0;
            }
            else if (labels.Any(x => unimportantSuffices.Any(y => x.EndsWith(y))))
            {
               return 2;
            }
            return 1;
         }

         return keys
            .SelectMany(fmk => fmk.MergeRequest.Labels)
            .GroupBy(
               label => label
                  .StartsWith(Constants.GitLabLabelPrefix) && label.IndexOf('-') != -1
                     ? label.Substring(0, label.IndexOf('-'))
                     : label,
               label => label,
               (baseLabel, labels) => new LabelGroup
               {
                  Labels = labels,
                  Priority = getPriority(labels)
               })
            .OrderBy(x => x.Priority)
            .Select(labelGroup => String.Format("{0}", String.Join(",", labelGroup.Labels.Distinct())));
      }

      public static IEnumerable<string> GroupLabels(FullMergeRequestKey fmk,
         IEnumerable<string> unimportantSuffices, User currentUser)
      {
         return GroupLabels(new FullMergeRequestKey[] { fmk }, unimportantSuffices, currentUser);
      }

      private static readonly Regex jira_re = new Regex(@"(?'name'(?!([A-Z0-9a-z]{1,10})-?$)[A-Z]{1}[A-Z0-9]+-\d+)");
      public static string GetJiraTask(MergeRequest mergeRequest)
      {
         Match m = jira_re.Match(mergeRequest.Title);
         return !m.Success || m.Groups.Count < 1 || !m.Groups["name"].Success ? String.Empty : m.Groups["name"].Value;
      }

      public static string GetJiraTaskUrl(MergeRequest mergeRequest, string jiraServiceUrl)
      {
         string jiraTask = GetJiraTask(mergeRequest);
         return jiraServiceUrl != String.Empty && jiraTask != String.Empty ?
            String.Format("{0}/browse/{1}", jiraServiceUrl, jiraTask) : String.Empty;
      }
   }
}

