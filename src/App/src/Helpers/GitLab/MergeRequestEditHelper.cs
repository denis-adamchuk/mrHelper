using GitLabSharp.Accessors;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient;
using System;
using System.Threading.Tasks;

namespace mrHelper.App.Helpers.GitLab
{
   internal static class MergeRequestEditHelper
   {
      async internal static Task<MergeRequest> SubmitNewMergeRequestAsync(GitLabInstance gitLabInstance,
         ProjectKey projectKey,
         string sourceBranch, string targetBranch, string title, string username, string description,
         bool deleteSourceBranch, bool squash)
      {
         if (String.IsNullOrEmpty(projectKey.ProjectName)
          || String.IsNullOrEmpty(sourceBranch)
          || String.IsNullOrEmpty(targetBranch)
          || String.IsNullOrEmpty(username) // TODO This is possible!
          || String.IsNullOrEmpty(title))
         {
            // TODO WTF Error handling
            return null;
         }

         User assignee = await getUserAsync(gitLabInstance, username);
         if (assignee == null)
         {
            // TODO WTF Error handling
            return null;
         }

         CreateNewMergeRequestParameters parameters = new CreateNewMergeRequestParameters(
            sourceBranch, targetBranch, title, assignee.Id, description, deleteSourceBranch, squash);
         return await Shortcuts.GetMergeRequestCreator(gitLabInstance, projectKey).CreateMergeRequest(parameters);
      }

      async internal static Task<bool> ApplyChangesToMergeRequest(GitLabInstance gitLabInstance,
         ProjectKey projectKey, MergeRequest mergeRequest, string targetBranch, string title, string username,
         string description, bool deleteSourceBranch, bool squash)
      {
         if (String.IsNullOrEmpty(targetBranch)
          || String.IsNullOrEmpty(username) // TODO This is possible
          || String.IsNullOrEmpty(title))
         {
            // TODO WTF Error handling
            return false;
         }

         string oldTargetBranch = mergeRequest.Target_Branch ?? String.Empty;
         string oldAssigneeUsername = mergeRequest.Assignee?.Username ?? String.Empty;
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
            return false;
         }

         User assignee = oldAssigneeUsername == username ?
            mergeRequest.Assignee : await getUserAsync(gitLabInstance, username);
         if (assignee == null)
         {
            // TODO WTF Error handling
            return false;
         }

         UpdateMergeRequestParameters updateMergeRequestParameters = new UpdateMergeRequestParameters(
            targetBranch, title, assignee.Id, description, null, deleteSourceBranch, squash);
         await Shortcuts
            .GetMergeRequestEditor(gitLabInstance, new MergeRequestKey(projectKey, mergeRequest.IId))
            .ModifyMergeRequest(updateMergeRequestParameters);
         return true;
      }

      async private static Task<User> getUserAsync(GitLabInstance gitLabInstance, string username)
      {
         GitLabClient.UserAccessor userAccessor = Shortcuts.GetUserAccessor(gitLabInstance);
         return await userAccessor.SearchUserByUsernameAsync(username)
             ?? await userAccessor.SearchUserByNameAsync(username); // fallback
      }
   }
}

