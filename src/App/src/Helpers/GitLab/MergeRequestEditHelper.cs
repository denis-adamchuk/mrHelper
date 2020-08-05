using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitLabSharp.Accessors;
using GitLabSharp.Entities;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient;

namespace mrHelper.App.Helpers.GitLab
{
   internal static class MergeRequestEditHelper
   {
      async internal static Task<string> GetLatestSpecialNote(IDiscussionCache discussionCache, MergeRequestKey mrk)
      {
         IEnumerable<Discussion> discussions = null;
         try
         {
            discussions = await discussionCache.LoadDiscussions(mrk);
         }
         catch (DiscussionCacheException ex)
         {
            ExceptionHandlers.Handle("Could not load Discussions", ex);
            return String.Empty;
         }

         Discussion note = discussions?
            .Where(x => x.Notes != null
                     && x.Notes.Count() == 1
                     && x.Notes.First().Body.StartsWith(Program.ServiceManager.GetSpecialNotePrefix()))
            .LastOrDefault();
         return note?.Notes.First().Body ?? String.Empty;
      }

      internal struct SubmitNewMergeRequestParameters
      {
         public SubmitNewMergeRequestParameters(ProjectKey projectKey, string sourceBranch, string targetBranch,
            string title, string assigneeUserName, string description, bool deleteSourceBranch, bool squash)
         {
            ProjectKey = projectKey;
            SourceBranch = sourceBranch;
            TargetBranch = targetBranch;
            Title = title;
            AssigneeUserName = assigneeUserName;
            Description = description;
            DeleteSourceBranch = deleteSourceBranch;
            Squash = squash;
         }

         internal ProjectKey ProjectKey { get; }
         internal string SourceBranch { get; }
         internal string TargetBranch { get; }
         internal string Title { get; }
         internal string AssigneeUserName { get; }
         internal string Description { get; }
         internal bool DeleteSourceBranch { get; }
         internal bool Squash { get; }
      }

      async internal static Task<MergeRequestKey?> SubmitNewMergeRequestAsync(GitLabInstance gitLabInstance,
         SubmitNewMergeRequestParameters parameters, string firstNote, User currentUser)
      {
         if (String.IsNullOrEmpty(parameters.ProjectKey.ProjectName)
          || String.IsNullOrEmpty(parameters.SourceBranch)
          || String.IsNullOrEmpty(parameters.TargetBranch)
          || String.IsNullOrEmpty(parameters.Title))
         {
            // this is unexpected due to UI restrictions, so don't implement detailed logging here
            MessageBox.Show("Invalid parameters for a new merge request", "Error",
               MessageBoxButtons.OK, MessageBoxIcon.Error);
            Trace.TraceError("[MergeRequestEditHelper] Invalid parameters for a new merge request");
            return null;
         }

         User assignee =  await getUserAsync(gitLabInstance, parameters.AssigneeUserName);
         if (!String.IsNullOrEmpty(parameters.AssigneeUserName) && assignee == null)
         {
            string message = String.Format("Cannot find user {0} at {1}, assignee field will be empty",
               parameters.AssigneeUserName, parameters.ProjectKey.HostName);
            MessageBox.Show(message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Trace.TraceWarning("[MergeRequestEditHelper] " + message);
         }

         CreateNewMergeRequestParameters creatorParameters = new CreateNewMergeRequestParameters(
            parameters.SourceBranch, parameters.TargetBranch, parameters.Title, assignee?.Id,
            parameters.Description, parameters.DeleteSourceBranch, parameters.Squash);
         MergeRequest mergeRequest = await Shortcuts.GetMergeRequestCreator(gitLabInstance, parameters.ProjectKey)
            .CreateMergeRequest(creatorParameters);
         if (mergeRequest == null)
         {
            MessageBox.Show("GitLab could not create a merge request with the given parameters", "Error",
               MessageBoxButtons.OK, MessageBoxIcon.Error);
            Trace.TraceError("[MergeRequestEditHelper] Failed to create a merge request");
            return null;
         }

         MergeRequestKey mrk = new MergeRequestKey(parameters.ProjectKey, mergeRequest.IId);
         if (!String.IsNullOrEmpty(firstNote))
         {
            await addComment(gitLabInstance, mrk, currentUser, firstNote);
         }
         return mrk;
      }

      internal struct ApplyMergeRequestChangesParameters
      {
         internal string Title { get; }
         internal string AssigneeUserName { get; }
         internal string Description { get; }
         internal bool DeleteSourceBranch { get; }
         internal bool Squash { get; }

         public ApplyMergeRequestChangesParameters(string title, string assigneeUserName, string description,
            bool deleteSourceBranch, bool squash)
         {
            Title = title;
            AssigneeUserName = assigneeUserName;
            Description = description;
            DeleteSourceBranch = deleteSourceBranch;
            Squash = squash;
         }
      }

      async internal static Task<bool> ApplyChangesToMergeRequest(GitLabInstance gitLabInstance,
         ProjectKey projectKey, MergeRequest originalMergeRequest, ApplyMergeRequestChangesParameters parameters,
         string oldSpecialNote, string newSpecialNote, User currentUser)
      {
         if (String.IsNullOrEmpty(parameters.Title))
         {
            // this is unexpected due to UI restrictions, so don't implement detailed logging here
            MessageBox.Show("Invalid parameters for a new merge request", "Error",
               MessageBoxButtons.OK, MessageBoxIcon.Error);
            Trace.TraceError("[MergeRequestEditHelper] Invalid parameters for a new merge request");
            return false;
         }

         string oldAssigneeUsername = originalMergeRequest.Assignee?.Username ?? String.Empty;
         User assignee = oldAssigneeUsername == parameters.AssigneeUserName
            ? originalMergeRequest.Assignee : await getUserAsync(gitLabInstance, parameters.AssigneeUserName);
         if (!String.IsNullOrEmpty(parameters.AssigneeUserName) && assignee == null)
         {
            string message = String.Format("Cannot find user {0} at {1}, assignee field will be empty",
               parameters.AssigneeUserName, projectKey.HostName);
            MessageBox.Show(message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Trace.TraceWarning("[MergeRequestEditHelper] " + message);
         }

         bool result = false;
         MergeRequestKey mrk = new MergeRequestKey(projectKey, originalMergeRequest.IId);
         if (oldSpecialNote != newSpecialNote)
         {
            result = await addComment(gitLabInstance, mrk, currentUser, newSpecialNote);
         }

         string oldTitle = originalMergeRequest.Title ?? String.Empty;
         string oldDescription = originalMergeRequest.Description ?? String.Empty;

         bool changed =
               oldAssigneeUsername != parameters.AssigneeUserName
            || originalMergeRequest.Force_Remove_Source_Branch != parameters.DeleteSourceBranch
            || originalMergeRequest.Squash != parameters.Squash
            || oldTitle != parameters.Title
            || oldDescription != parameters.Description;
         if (!changed)
         {
            return result;
         }

         UpdateMergeRequestParameters updateMergeRequestParameters = new UpdateMergeRequestParameters(
            null, parameters.Title, assignee?.Id, parameters.Description, null, parameters.DeleteSourceBranch,
            parameters.Squash);
         MergeRequest mergeRequest = await Shortcuts
            .GetMergeRequestEditor(gitLabInstance, mrk)
            .ModifyMergeRequest(updateMergeRequestParameters);
         if (mergeRequest == null)
         {
            MessageBox.Show("GitLab could not modify the selected merge request", "Error",
               MessageBoxButtons.OK, MessageBoxIcon.Error);
            Trace.TraceError("[MergeRequestEditHelper] Failed to modify a merge request");
            return result;
         }
         return true;
      }

      async private static Task<User> getUserAsync(GitLabInstance gitLabInstance, string username)
      {
         if (String.IsNullOrEmpty(username))
         {
            return null;
         }

         GitLabClient.UserAccessor userAccessor = Shortcuts.GetUserAccessor(gitLabInstance);
         return await userAccessor.SearchUserByUsernameAsync(username)
             ?? await userAccessor.SearchUserByNameAsync(username); // fallback
      }

      async private static Task<bool> addComment(GitLabInstance gitLabInstance, MergeRequestKey mrk, User currentUser,
         string commentBody)
      {
         try
         {
            IDiscussionCreator creator = Shortcuts.GetDiscussionCreator(gitLabInstance, mrk, currentUser);
            await creator.CreateNoteAsync(new CreateNewNoteParameters(commentBody));
            return true;
         }
         catch (DiscussionCreatorException ex)
         {
            MessageBox.Show("Failed to create a note in the new merge request", "Error",
               MessageBoxButtons.OK, MessageBoxIcon.Error);
            ExceptionHandlers.Handle("Failed to create a note", ex);
         }
         return false;
      }
   }
}

