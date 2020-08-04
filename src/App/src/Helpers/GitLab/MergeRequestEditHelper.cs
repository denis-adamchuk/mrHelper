using GitLabSharp.Accessors;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace mrHelper.App.Helpers.GitLab
{
   internal static class MergeRequestEditHelper
   {
      async internal static Task<MergeRequestKey?> SubmitNewMergeRequestAsync(GitLabInstance gitLabInstance,
         ProjectKey projectKey, string sourceBranch, string targetBranch, string title, string assigneeUsername,
         string description, bool deleteSourceBranch, bool squash, string specialNote, User currentUser)
      {
         if (String.IsNullOrEmpty(projectKey.ProjectName)
          || String.IsNullOrEmpty(sourceBranch)
          || String.IsNullOrEmpty(targetBranch)
          || String.IsNullOrEmpty(assigneeUsername) // TODO This is possible!
          || String.IsNullOrEmpty(title))
         {
            // TODO WTF Error handling
            return null;
         }

         User assignee = await getUserAsync(gitLabInstance, assigneeUsername);
         if (assignee == null)
         {
            // TODO WTF Error handling
            return null;
         }

         CreateNewMergeRequestParameters parameters = new CreateNewMergeRequestParameters(
            sourceBranch, targetBranch, title, assignee.Id, description, deleteSourceBranch, squash);
         MergeRequest mergeRequest = await Shortcuts.GetMergeRequestCreator(gitLabInstance, projectKey)
            .CreateMergeRequest(parameters);
         if (mergeRequest == null)
         {
            return null;
         }

         MergeRequestKey mrk = new MergeRequestKey(projectKey, mergeRequest.IId);
         if (!String.IsNullOrEmpty(specialNote))
         {
            await addComment(gitLabInstance, mrk, currentUser, specialNote);
         }
         return mrk;
      }

      async internal static Task<bool> ApplyChangesToMergeRequest(GitLabInstance gitLabInstance,
         ProjectKey projectKey, MergeRequest mergeRequest, string targetBranch, string title, string username,
         string description, bool deleteSourceBranch, bool squash, string oldSpecialNote, string newSpecialNote,
         User currentUser)
      {
         if (String.IsNullOrEmpty(targetBranch)
          || String.IsNullOrEmpty(username) // TODO This is possible
          || String.IsNullOrEmpty(title))
         {
            // TODO WTF Error handling
            return false;
         }

         string oldAssigneeUsername = mergeRequest.Assignee?.Username ?? String.Empty;
         User assignee = oldAssigneeUsername == username
            ? mergeRequest.Assignee : await getUserAsync(gitLabInstance, username);
         if (assignee == null)
         {
            // TODO WTF Error handling
            return false;
         }

         bool result = false;
         MergeRequestKey mrk = new MergeRequestKey(projectKey, mergeRequest.IId);
         if (oldSpecialNote != newSpecialNote)
         {
            await addComment(gitLabInstance, mrk, currentUser, newSpecialNote);
            result = true;
         }

         string oldTargetBranch = mergeRequest.Target_Branch ?? String.Empty;
         bool oldDeleteSourceBranch = mergeRequest.Force_Remove_Source_Branch;
         bool oldSquash = mergeRequest.Squash;
         string oldTitle = mergeRequest.Title;
         string oldDescription = mergeRequest.Description;

         bool changed =
               oldTargetBranch != targetBranch
            || oldAssigneeUsername != username
            || oldDeleteSourceBranch != deleteSourceBranch
            || oldSquash != squash
            || oldTitle != title
            || oldDescription != description;
         if (!changed)
         {
            return result;
         }

         UpdateMergeRequestParameters updateMergeRequestParameters = new UpdateMergeRequestParameters(
            targetBranch, title, assignee.Id, description, null, deleteSourceBranch, squash);
         await Shortcuts
            .GetMergeRequestEditor(gitLabInstance, mrk)
            .ModifyMergeRequest(updateMergeRequestParameters);
         return true;
      }

      async private static Task<User> getUserAsync(GitLabInstance gitLabInstance, string username)
      {
         GitLabClient.UserAccessor userAccessor = Shortcuts.GetUserAccessor(gitLabInstance);
         return await userAccessor.SearchUserByUsernameAsync(username)
             ?? await userAccessor.SearchUserByNameAsync(username); // fallback
      }

      async private static Task addComment(GitLabInstance gitLabInstance, MergeRequestKey mrk, User currentUser,
         string commentBody)
      {
         try
         {
            IDiscussionCreator creator = Shortcuts.GetDiscussionCreator(gitLabInstance, mrk, currentUser);
            await creator.CreateNoteAsync(new CreateNewNoteParameters(commentBody));
         }
         catch (DiscussionCreatorException)
         {
            MessageBox.Show("Failed to create a note in the new merge request", "Error",
               MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }
   }
}

