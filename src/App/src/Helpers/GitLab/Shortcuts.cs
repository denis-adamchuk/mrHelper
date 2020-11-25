﻿using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient;

namespace mrHelper.App.Helpers.GitLab
{
   internal static class Shortcuts
   {
      internal static ProjectAccessor GetProjectAccessor(GitLabInstance gitLabInstance,
         IModificationListener modificationListener)
      {
         return new RawDataAccessor(gitLabInstance)
            .GetProjectAccessor(modificationListener);
      }

      internal static UserAccessor GetUserAccessor(GitLabInstance gitLabInstance)
      {
         return new RawDataAccessor(gitLabInstance)
            .UserAccessor;
      }

      internal static MergeRequestAccessor GetMergeRequestAccessor(GitLabInstance gitLabInstance,
         IModificationListener modificationListener, ProjectKey projectKey)
      {
         return new RawDataAccessor(gitLabInstance)
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
         IModificationListener modificationListener, ProjectKey projectKey)
      {
         return GetMergeRequestAccessor(gitLabInstance, modificationListener, projectKey)
            .GetMergeRequestCreator();
      }

      internal static IMergeRequestEditor GetMergeRequestEditor(GitLabInstance gitLabInstance,
         IModificationListener modificationListener, MergeRequestKey mrk)
      {
         return GetMergeRequestAccessor(gitLabInstance, modificationListener, mrk.ProjectKey)
            .GetSingleMergeRequestAccessor(mrk.IId)
            .GetMergeRequestEditor();
      }

      internal static ITimeTracker GetTimeTracker(GitLabInstance gitLabInstance,
         IModificationListener modificationListener, MergeRequestKey mrk)
      {
         return GetMergeRequestAccessor(gitLabInstance, modificationListener, mrk.ProjectKey)
            .GetSingleMergeRequestAccessor(mrk.IId).GetTimeTracker();
      }

      internal static IDiscussionCreator GetDiscussionCreator(GitLabInstance gitLabInstance,
         IModificationListener modificationListener, MergeRequestKey mrk, User user)
      {
         return GetMergeRequestAccessor(gitLabInstance, modificationListener, mrk.ProjectKey)
            .GetSingleMergeRequestAccessor(mrk.IId)
            .GetDiscussionAccessor()
            .GetDiscussionCreator(user);
      }

      internal static SingleDiscussionAccessor GetSingleDiscussionAccessor(GitLabInstance gitLabInstance,
         IModificationListener modificationListener, MergeRequestKey mrk, string discussionId)
      {
         return GetMergeRequestAccessor(gitLabInstance, modificationListener, mrk.ProjectKey)
            .GetSingleMergeRequestAccessor(mrk.IId)
            .GetDiscussionAccessor()
            .GetSingleDiscussionAccessor(discussionId);
      }

      internal static IDiscussionEditor GetDiscussionEditor(GitLabInstance gitLabInstance,
         IModificationListener modificationListener, MergeRequestKey mrk, string discussionId)
      {
         return GetMergeRequestAccessor(gitLabInstance, modificationListener, mrk.ProjectKey)
            .GetSingleMergeRequestAccessor(mrk.IId)
            .GetDiscussionAccessor()
            .GetSingleDiscussionAccessor(discussionId)
            .GetDiscussionEditor();
      }
   }
}

