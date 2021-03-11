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
   internal struct SubmitNewMergeRequestParameters
   {
      public SubmitNewMergeRequestParameters(ProjectKey projectKey, string sourceBranch, string targetBranch,
         string title, string assigneeUserName, string description, bool deleteSourceBranch, bool squash,
         bool isHighPriority)
      {
         ProjectKey = projectKey;
         SourceBranch = sourceBranch;
         TargetBranch = targetBranch;
         Title = title;
         AssigneeUserName = assigneeUserName;
         Description = description;
         DeleteSourceBranch = deleteSourceBranch;
         Squash = squash;
         IsHighPriority = isHighPriority;
      }

      internal ProjectKey ProjectKey { get; }
      internal string SourceBranch { get; }
      internal string TargetBranch { get; }
      internal string Title { get; }
      internal string AssigneeUserName { get; }
      internal string Description { get; }
      internal bool DeleteSourceBranch { get; }
      internal bool Squash { get; }
      internal bool IsHighPriority { get; }
   }

   internal struct ApplyMergeRequestChangesParameters
   {
      public ApplyMergeRequestChangesParameters(string title, string assigneeUserName, string description,
         string targetBranch, bool deleteSourceBranch, bool squash, bool isHighPriority)
      {
         Title = title;
         AssigneeUserName = assigneeUserName;
         Description = description;
         TargetBranch = targetBranch;
         DeleteSourceBranch = deleteSourceBranch;
         Squash = squash;
         IsHighPriority = isHighPriority;
      }

      internal string Title { get; }
      internal string AssigneeUserName { get; }
      internal string Description { get; }
      internal string TargetBranch { get; }
      internal bool DeleteSourceBranch { get; }
      internal bool Squash { get; }
      internal bool IsHighPriority { get; }
   }

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
         return note?.Notes.First().Body;
      }

      async internal static Task<MergeRequestKey?> SubmitNewMergeRequestAsync(
         SubmitNewMergeRequestParameters parameters, string firstNote, User currentUser, Shortcuts shortcuts)
      {
         if (String.IsNullOrEmpty(parameters.ProjectKey.ProjectName)
          || String.IsNullOrEmpty(parameters.SourceBranch)
          || String.IsNullOrEmpty(parameters.TargetBranch)
          || String.IsNullOrEmpty(parameters.Title)
          || parameters.AssigneeUserName == null
          || firstNote == null
          || currentUser == null)
         {
            // this is unexpected due to UI restrictions, so don't implement detailed logging here
            MessageBox.Show("Invalid parameters for a new merge request", "Error",
               MessageBoxButtons.OK, MessageBoxIcon.Error);
            Trace.TraceError("[MergeRequestEditHelper] Invalid parameters for a new merge request");
            return null;
         }

         User assignee = await getUserAsync(parameters.AssigneeUserName, shortcuts);
         checkFoundAssignee(parameters.ProjectKey.HostName, parameters.AssigneeUserName, assignee);

         string[] labels = parameters.IsHighPriority
            ? new string[] { Common.Constants.Constants.HighPriorityLabel } : null;
         int assigneeId = assignee?.Id ?? 0; // 0 means to not assign MR to anyone
         CreateNewMergeRequestParameters creatorParameters = new CreateNewMergeRequestParameters(
            parameters.SourceBranch, parameters.TargetBranch, parameters.Title, assigneeId,
            parameters.Description, parameters.DeleteSourceBranch, parameters.Squash, labels);

         MergeRequest mergeRequest;
         try
         {
            mergeRequest = await shortcuts
               .GetMergeRequestCreator(parameters.ProjectKey)
               .CreateMergeRequest(creatorParameters);
         }
         catch (MergeRequestCreatorException ex)
         {
            reportErrorToUser(ex);
            return null;
         }

         MergeRequestKey mrk = new MergeRequestKey(parameters.ProjectKey, mergeRequest.IId);
         await addComment(mrk, currentUser, firstNote, shortcuts);
         return mrk;
      }

      async internal static Task<bool> ApplyChangesToMergeRequest(
         ProjectKey projectKey, MergeRequest originalMergeRequest, ApplyMergeRequestChangesParameters parameters,
         string oldSpecialNote, string newSpecialNote, User currentUser, Shortcuts shortcuts)
      {
         if (String.IsNullOrEmpty(parameters.Title)
          || String.IsNullOrEmpty(parameters.TargetBranch)
          || parameters.AssigneeUserName == null
          || newSpecialNote == null
          || currentUser == null)
         {
            // this is unexpected due to UI restrictions, so don't implement detailed logging here
            MessageBox.Show("Invalid parameters for a merge request", "Error",
               MessageBoxButtons.OK, MessageBoxIcon.Error);
            Trace.TraceError("[MergeRequestEditHelper] Invalid parameters for a merge request");
            return false;
         }

         string oldAssigneeUsername = originalMergeRequest.Assignee?.Username ?? String.Empty;
         User assignee = oldAssigneeUsername == parameters.AssigneeUserName
            ? originalMergeRequest.Assignee
            : await getUserAsync(parameters.AssigneeUserName, shortcuts);
         checkFoundAssignee(projectKey.HostName, parameters.AssigneeUserName, assignee);

         bool result = false;
         MergeRequestKey mrk = new MergeRequestKey(projectKey, originalMergeRequest.IId);
         if (oldSpecialNote != newSpecialNote)
         {
            result = await addComment(mrk, currentUser, newSpecialNote, shortcuts);
         }

         bool wasHighPriority = originalMergeRequest.Labels?
            .Contains(Common.Constants.Constants.HighPriorityLabel) ?? false;

         bool changed =
               oldAssigneeUsername != parameters.AssigneeUserName
            || originalMergeRequest.Force_Remove_Source_Branch != parameters.DeleteSourceBranch
            || originalMergeRequest.Squash != parameters.Squash
            || originalMergeRequest.Target_Branch != parameters.TargetBranch
            || originalMergeRequest.Title != parameters.Title
            || originalMergeRequest.Description != parameters.Description
            || wasHighPriority != parameters.IsHighPriority;
         if (!changed)
         {
            return result;
         }

         string[] labels = null;
         if (wasHighPriority && !parameters.IsHighPriority)
         {
            labels = originalMergeRequest.Labels
               .Where(label => label != Common.Constants.Constants.HighPriorityLabel).ToArray();
         }
         else if (!wasHighPriority && parameters.IsHighPriority)
         {
            labels = originalMergeRequest.Labels
               .Concat(new string[] { Common.Constants.Constants.HighPriorityLabel }).ToArray();
         }

         int assigneeId = assignee?.Id ?? 0; // 0 means to unassign
         UpdateMergeRequestParameters updateMergeRequestParameters = new UpdateMergeRequestParameters(
            parameters.TargetBranch, parameters.Title, assigneeId, parameters.Description,
            null, parameters.DeleteSourceBranch, parameters.Squash, labels);
         try
         {
            MergeRequest mergeRequest = await shortcuts
               .GetMergeRequestEditor(mrk)
               .ModifyMergeRequest(updateMergeRequestParameters);
         }
         catch (MergeRequestEditorException ex)
         {
            reportErrorToUser(ex);
            return result;
         }
         return true;
      }

      async internal static Task<bool> CloseMergeRequest(MergeRequestKey mrk, Shortcuts shortcuts)
      {
         UpdateMergeRequestParameters updateMergeRequestParameters = new UpdateMergeRequestParameters(
            null, null, null, null, "close", null, null, null);
         try
         {
            MergeRequest mergeRequest = await shortcuts
               .GetMergeRequestEditor(mrk)
               .ModifyMergeRequest(updateMergeRequestParameters);
         }
         catch (MergeRequestEditorException ex)
         {
            reportErrorToUser(ex);
            return false;
         }
         return true;
      }

      async private static Task<User> getUserAsync(string username, Shortcuts shortcuts)
      {
         if (String.IsNullOrEmpty(username))
         {
            return null;
         }

         GitLabClient.UserAccessor userAccessor = shortcuts.GetUserAccessor();
         return await userAccessor.SearchUserByUsernameAsync(username)
             ?? await userAccessor.SearchUserByNameAsync(username); // fallback
      }

      private static void checkFoundAssignee(string hostname, string username, User foundAssignee)
      {
         if (!String.IsNullOrEmpty(username) && foundAssignee == null)
         {
            string message = String.Format(
               "Cannot find user {0} at {1}, assignee field will be empty", username, hostname);
            MessageBox.Show(message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Trace.TraceWarning("[MergeRequestEditHelper] " + message);
         }
      }

      async private static Task<bool> addComment(
         MergeRequestKey mrk, User currentUser, string commentBody, Shortcuts shortcuts)
      {
         if (String.IsNullOrEmpty(commentBody))
         {
            return false;
         }

         try
         {
            IDiscussionCreator creator = shortcuts.GetDiscussionCreator(mrk, currentUser);
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

      private static void reportErrorToUser(Exception ex)
      {
         if (ex is MergeRequestCreatorCancelledException || ex is MergeRequestEditorCancelledException)
         {
            Trace.TraceWarning("[MergeRequestEditHelper] Cancelled operation");
            return;
         }

         void showDialogAndLogError(string message = "Unknown")
         {
            string defaultMessage = "GitLab could not create a merge request with the given parameters. Reason: ";
            MessageBox.Show(defaultMessage + message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Trace.TraceError("[MergeRequestEditHelper] " + defaultMessage + message);
         };

         if (ex.InnerException != null && (ex.InnerException is GitLabRequestException))
         {
            GitLabRequestException rx = ex.InnerException as GitLabRequestException;
            if (rx.InnerException is System.Net.WebException wx && wx.Response != null)
            {
               System.Net.HttpWebResponse response = wx.Response as System.Net.HttpWebResponse;
               const int unprocessableEntity = 422;
               switch (response.StatusCode)
               {
                  case System.Net.HttpStatusCode.Conflict:
                     showDialogAndLogError("Another open merge request already exists for this source branch");
                     return;

                  case System.Net.HttpStatusCode.Forbidden:
                     showDialogAndLogError("Access denied");
                     return;

                  case System.Net.HttpStatusCode.BadRequest:
                     showDialogAndLogError("Bad parameters");
                     return;

                  case (System.Net.HttpStatusCode)unprocessableEntity:
                     showDialogAndLogError("You can't use same project/branch for source and target");
                     return;
               }
            }
         }

         showDialogAndLogError();
      }
   }
}

