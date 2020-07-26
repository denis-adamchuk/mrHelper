using GitLabSharp.Entities;
using mrHelper.Client.Common;
using mrHelper.Client.Discussions;
using mrHelper.Client.MergeRequests;
using mrHelper.Client.Projects;
using mrHelper.Client.Types;
using mrHelper.Common.Interfaces;

namespace mrHelper.App.Helpers.GitLab
{
   internal static class Shortcuts
   {
      internal static IDiscussionCreator GetDiscussionCreator(IGitLabAccessor gitLabAccessor,
         MergeRequestKey mrk, User user)
      {
         ISingleMergeRequestAccessor singleMergeRequestAccessor =
            getMergeRequestAccessor(gitLabAccessor, mrk.ProjectKey).GetSingleMergeRequestAccessor(mrk.IId);
         return singleMergeRequestAccessor.GetDiscussionAccessor().GetDiscussionCreator(user);
      }

      internal static IDiscussionEditor GetDiscussionEditor(IGitLabAccessor gitLabAccessor,
         MergeRequestKey mrk, string discussionId)
      {
         ISingleMergeRequestAccessor singleMergeRequestAccessor =
            getMergeRequestAccessor(gitLabAccessor, mrk.ProjectKey).GetSingleMergeRequestAccessor(mrk.IId);
         IDiscussionAccessor discussionAccessor = singleMergeRequestAccessor.GetDiscussionAccessor();
         return discussionAccessor.GetSingleDiscussionAccessor(discussionId).GetDiscussionEditor();
      }

      internal static IMergeRequestCreator GetMergeRequestCreator(IGitLabAccessor gitLabAccessor,
         ProjectKey projectKey)
      {
         return getMergeRequestAccessor(gitLabAccessor, projectKey).GetMergeRequestCreator();
      }

      private static IMergeRequestAccessor getMergeRequestAccessor(IGitLabAccessor gitLabAccessor,
         ProjectKey projectKey)
      {
         IGitLabInstanceAccessor gitLabInstanceAccessor =
            gitLabAccessor.GetInstanceAccessor(projectKey.HostName);
         IProjectAccessor projectAccessor =
            gitLabInstanceAccessor.ProjectAccessor;
         ISingleProjectAccessor singleProjectAccessor =
            projectAccessor.GetSingleProjectAccessor(projectKey.ProjectName);
         return singleProjectAccessor.MergeRequestAccessor;
      }
   }
}

