using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient;
using mrHelper.GitLabClient.Interfaces;

namespace mrHelper.App.Helpers.GitLab
{
   internal static class Shortcuts
   {
      internal static ProjectAccessor GetProjectAccessor(GitLabInstance gitLabInstance,
         IModificationListener modificationListener, INetworkOperationStatusListener networkOperationStatusListener)
      {
         return new RawDataAccessor(gitLabInstance, networkOperationStatusListener)
            .GetProjectAccessor(modificationListener);
      }

      internal static UserAccessor GetUserAccessor(GitLabInstance gitLabInstance,
         INetworkOperationStatusListener networkOperationStatusListener)
      {
         return new RawDataAccessor(gitLabInstance, networkOperationStatusListener)
            .UserAccessor;
      }

      internal static MergeRequestAccessor GetMergeRequestAccessor(GitLabInstance gitLabInstance,
         IModificationListener modificationListener, ProjectKey projectKey,
         INetworkOperationStatusListener networkOperationStatusListener)
      {
         return new RawDataAccessor(gitLabInstance, networkOperationStatusListener)
            .GetProjectAccessor(modificationListener)
            .GetSingleProjectAccessor(projectKey.ProjectName)
            .MergeRequestAccessor;
      }

      internal static MergeRequestAccessor GetMergeRequestAccessor(ProjectAccessor projectAccessor, string projectName)
      {
         return projectAccessor
            .GetSingleProjectAccessor(projectName)
            .MergeRequestAccessor;
      }

      internal static IMergeRequestCreator GetMergeRequestCreator(GitLabInstance gitLabInstance,
         IModificationListener modificationListener, ProjectKey projectKey,
         INetworkOperationStatusListener networkOperationStatusListener)
      {
         return GetMergeRequestAccessor(gitLabInstance, modificationListener, projectKey, networkOperationStatusListener)
            .GetMergeRequestCreator();
      }

      internal static IMergeRequestEditor GetMergeRequestEditor(GitLabInstance gitLabInstance,
         IModificationListener modificationListener, MergeRequestKey mrk,
         INetworkOperationStatusListener networkOperationStatusListener)
      {
         return GetMergeRequestAccessor(gitLabInstance, modificationListener, mrk.ProjectKey, networkOperationStatusListener)
            .GetSingleMergeRequestAccessor(mrk.IId)
            .GetMergeRequestEditor();
      }

      internal static ITimeTracker GetTimeTracker(GitLabInstance gitLabInstance,
         IModificationListener modificationListener, MergeRequestKey mrk,
         INetworkOperationStatusListener networkOperationStatusListener)
      {
         return GetMergeRequestAccessor(gitLabInstance, modificationListener, mrk.ProjectKey, networkOperationStatusListener)
            .GetSingleMergeRequestAccessor(mrk.IId).GetTimeTracker();
      }

      internal static IDiscussionCreator GetDiscussionCreator(GitLabInstance gitLabInstance,
         IModificationListener modificationListener, MergeRequestKey mrk, User user,
         INetworkOperationStatusListener networkOperationStatusListener)
      {
         return GetMergeRequestAccessor(gitLabInstance, modificationListener, mrk.ProjectKey, networkOperationStatusListener)
            .GetSingleMergeRequestAccessor(mrk.IId)
            .GetDiscussionAccessor()
            .GetDiscussionCreator(user);
      }

      internal static SingleDiscussionAccessor GetSingleDiscussionAccessor(GitLabInstance gitLabInstance,
         IModificationListener modificationListener, MergeRequestKey mrk, string discussionId,
         INetworkOperationStatusListener networkOperationStatusListener)
      {
         return GetMergeRequestAccessor(gitLabInstance, modificationListener, mrk.ProjectKey, networkOperationStatusListener)
            .GetSingleMergeRequestAccessor(mrk.IId)
            .GetDiscussionAccessor()
            .GetSingleDiscussionAccessor(discussionId);
      }

      internal static IDiscussionEditor GetDiscussionEditor(GitLabInstance gitLabInstance,
         IModificationListener modificationListener, MergeRequestKey mrk, string discussionId,
         INetworkOperationStatusListener networkOperationStatusListener)
      {
         return GetMergeRequestAccessor(gitLabInstance, modificationListener, mrk.ProjectKey, networkOperationStatusListener)
            .GetSingleMergeRequestAccessor(mrk.IId)
            .GetDiscussionAccessor()
            .GetSingleDiscussionAccessor(discussionId)
            .GetDiscussionEditor();
      }
   }
}

