using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient;
using mrHelper.GitLabClient.Interfaces;

namespace mrHelper.App.Helpers.GitLab
{
   internal class Shortcuts
   {
      internal Shortcuts(IModificationListener modificationListener,
                         INetworkOperationStatusListener networkOperationStatusListener)
      {
         _modificationListener = modificationListener;
         _networkOperationStatusListener = networkOperationStatusListener;
      }

      internal ProjectAccessor GetProjectAccessor(GitLabInstance gitLabInstance)
      {
         return new RawDataAccessor(gitLabInstance, _networkOperationStatusListener)
            .GetProjectAccessor(_modificationListener);
      }

      internal UserAccessor GetUserAccessor(GitLabInstance gitLabInstance)
      {
         return new RawDataAccessor(gitLabInstance, _networkOperationStatusListener)
            .UserAccessor;
      }

      internal MergeRequestAccessor GetMergeRequestAccessor(GitLabInstance gitLabInstance, ProjectKey projectKey)
      {
         return new RawDataAccessor(gitLabInstance, _networkOperationStatusListener)
            .GetProjectAccessor(_modificationListener)
            .GetSingleProjectAccessor(projectKey.ProjectName)
            .MergeRequestAccessor;
      }

      internal MergeRequestAccessor GetMergeRequestAccessor(ProjectAccessor projectAccessor, string projectName)
      {
         return projectAccessor
            .GetSingleProjectAccessor(projectName)
            .MergeRequestAccessor;
      }

      internal IMergeRequestCreator GetMergeRequestCreator(GitLabInstance gitLabInstance, ProjectKey projectKey)
      {
         return GetMergeRequestAccessor(gitLabInstance, projectKey)
            .GetMergeRequestCreator();
      }

      internal IMergeRequestEditor GetMergeRequestEditor(GitLabInstance gitLabInstance, MergeRequestKey mrk)
      {
         return GetMergeRequestAccessor(gitLabInstance, mrk.ProjectKey)
            .GetSingleMergeRequestAccessor(mrk.IId)
            .GetMergeRequestEditor();
      }

      internal ITimeTracker GetTimeTracker(GitLabInstance gitLabInstance, MergeRequestKey mrk)
      {
         return GetMergeRequestAccessor(gitLabInstance, mrk.ProjectKey)
            .GetSingleMergeRequestAccessor(mrk.IId).GetTimeTracker();
      }

      internal IDiscussionCreator GetDiscussionCreator(GitLabInstance gitLabInstance, MergeRequestKey mrk, User user)
      {
         return GetMergeRequestAccessor(gitLabInstance, mrk.ProjectKey)
            .GetSingleMergeRequestAccessor(mrk.IId)
            .GetDiscussionAccessor()
            .GetDiscussionCreator(user);
      }

      internal SingleDiscussionAccessor GetSingleDiscussionAccessor(GitLabInstance gitLabInstance,
         MergeRequestKey mrk, string discussionId)
      {
         return GetMergeRequestAccessor(gitLabInstance, mrk.ProjectKey)
            .GetSingleMergeRequestAccessor(mrk.IId)
            .GetDiscussionAccessor()
            .GetSingleDiscussionAccessor(discussionId);
      }

      internal IDiscussionEditor GetDiscussionEditor(GitLabInstance gitLabInstance, MergeRequestKey mrk,
         string discussionId)
      {
         return GetMergeRequestAccessor(gitLabInstance, mrk.ProjectKey)
            .GetSingleMergeRequestAccessor(mrk.IId)
            .GetDiscussionAccessor()
            .GetSingleDiscussionAccessor(discussionId)
            .GetDiscussionEditor();
      }

      private readonly IModificationListener _modificationListener;
      private readonly INetworkOperationStatusListener _networkOperationStatusListener;
   }
}

