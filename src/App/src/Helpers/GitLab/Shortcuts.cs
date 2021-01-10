using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient;
using mrHelper.GitLabClient.Interfaces;

namespace mrHelper.App.Helpers.GitLab
{
   internal static class Shortcuts
   {
      internal static ProjectAccessor GetProjectAccessor(GitLabInstance gitLabInstance,
         IModificationListener modificationListener, IConnectionLossListener connectionLossListener)
      {
         return new RawDataAccessor(gitLabInstance, connectionLossListener)
            .GetProjectAccessor(modificationListener);
      }

      internal static UserAccessor GetUserAccessor(GitLabInstance gitLabInstance,
         IConnectionLossListener connectionLossListener)
      {
         return new RawDataAccessor(gitLabInstance, connectionLossListener)
            .UserAccessor;
      }

      internal static MergeRequestAccessor GetMergeRequestAccessor(GitLabInstance gitLabInstance,
         IModificationListener modificationListener, ProjectKey projectKey,
         IConnectionLossListener connectionLossListener)
      {
         return new RawDataAccessor(gitLabInstance, connectionLossListener)
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
         IConnectionLossListener connectionLossListener)
      {
         return GetMergeRequestAccessor(gitLabInstance, modificationListener, projectKey, connectionLossListener)
            .GetMergeRequestCreator();
      }

      internal static IMergeRequestEditor GetMergeRequestEditor(GitLabInstance gitLabInstance,
         IModificationListener modificationListener, MergeRequestKey mrk,
         IConnectionLossListener connectionLossListener)
      {
         return GetMergeRequestAccessor(gitLabInstance, modificationListener, mrk.ProjectKey, connectionLossListener)
            .GetSingleMergeRequestAccessor(mrk.IId)
            .GetMergeRequestEditor();
      }

      internal static ITimeTracker GetTimeTracker(GitLabInstance gitLabInstance,
         IModificationListener modificationListener, MergeRequestKey mrk,
         IConnectionLossListener connectionLossListener)
      {
         return GetMergeRequestAccessor(gitLabInstance, modificationListener, mrk.ProjectKey, connectionLossListener)
            .GetSingleMergeRequestAccessor(mrk.IId).GetTimeTracker();
      }

      internal static IDiscussionCreator GetDiscussionCreator(GitLabInstance gitLabInstance,
         IModificationListener modificationListener, MergeRequestKey mrk, User user,
         IConnectionLossListener connectionLossListener)
      {
         return GetMergeRequestAccessor(gitLabInstance, modificationListener, mrk.ProjectKey, connectionLossListener)
            .GetSingleMergeRequestAccessor(mrk.IId)
            .GetDiscussionAccessor()
            .GetDiscussionCreator(user);
      }

      internal static SingleDiscussionAccessor GetSingleDiscussionAccessor(GitLabInstance gitLabInstance,
         IModificationListener modificationListener, MergeRequestKey mrk, string discussionId,
         IConnectionLossListener connectionLossListener)
      {
         return GetMergeRequestAccessor(gitLabInstance, modificationListener, mrk.ProjectKey, connectionLossListener)
            .GetSingleMergeRequestAccessor(mrk.IId)
            .GetDiscussionAccessor()
            .GetSingleDiscussionAccessor(discussionId);
      }

      internal static IDiscussionEditor GetDiscussionEditor(GitLabInstance gitLabInstance,
         IModificationListener modificationListener, MergeRequestKey mrk, string discussionId,
         IConnectionLossListener connectionLossListener)
      {
         return GetMergeRequestAccessor(gitLabInstance, modificationListener, mrk.ProjectKey, connectionLossListener)
            .GetSingleMergeRequestAccessor(mrk.IId)
            .GetDiscussionAccessor()
            .GetSingleDiscussionAccessor(discussionId)
            .GetDiscussionEditor();
      }
   }
}

