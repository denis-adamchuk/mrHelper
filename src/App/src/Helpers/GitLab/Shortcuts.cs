using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient;
using System;

namespace mrHelper.App.Helpers.GitLab
{
   public class Shortcuts
   {
      public Shortcuts(GitLabInstance gitLabIntance)
      {
         _gitLabInstance = gitLabIntance;
      }

      internal GitLabVersionAccessor GetGitLabVersionAccessor()
      {
         return new RawDataAccessor(_gitLabInstance)
            .VersionAccessor;
      }

      internal PersonalAccessTokenAccessor GetPersonalAccessTokenAccessor()
      {
         return new RawDataAccessor(_gitLabInstance)
            .AccessTokenAccessor;
      }

      internal ProjectAccessor GetProjectAccessor()
      {
         return new RawDataAccessor(_gitLabInstance)
            .ProjectAccessor;
      }

      internal UserAccessor GetUserAccessor()
      {
         return new RawDataAccessor(_gitLabInstance)
            .UserAccessor;
      }

      internal MergeRequestAccessor GetMergeRequestAccessor(ProjectKey projectKey)
      {
         return GetMergeRequestAccessor(projectKey.ProjectName);
      }

      internal MergeRequestAccessor GetMergeRequestAccessor(string projectName)
      {
         return GetProjectAccessor()
            .GetSingleProjectAccessor(projectName)
            .MergeRequestAccessor;
      }

      internal IMergeRequestCreator GetMergeRequestCreator(ProjectKey projectKey)
      {
         return GetMergeRequestAccessor(projectKey)
            .GetMergeRequestCreator();
      }

      internal IMergeRequestEditor GetMergeRequestEditor(MergeRequestKey mrk)
      {
         return GetMergeRequestAccessor(mrk.ProjectKey)
            .GetSingleMergeRequestAccessor(mrk.IId)
            .GetMergeRequestEditor();
      }

      internal ITimeTracker GetTimeTracker(MergeRequestKey mrk)
      {
         return GetMergeRequestAccessor(mrk.ProjectKey)
            .GetSingleMergeRequestAccessor(mrk.IId).GetTimeTracker();
      }

      internal IDiscussionCreator GetDiscussionCreator(MergeRequestKey mrk, User user)
      {
         return GetMergeRequestAccessor(mrk.ProjectKey)
            .GetSingleMergeRequestAccessor(mrk.IId)
            .GetDiscussionAccessor()
            .GetDiscussionCreator(user);
      }

      internal SingleDiscussionAccessor GetSingleDiscussionAccessor(MergeRequestKey mrk, string discussionId)
      {
         return GetMergeRequestAccessor(mrk.ProjectKey)
            .GetSingleMergeRequestAccessor(mrk.IId)
            .GetDiscussionAccessor()
            .GetSingleDiscussionAccessor(discussionId);
      }

      internal IDiscussionEditor GetDiscussionEditor(MergeRequestKey mrk, string discussionId)
      {
         return GetMergeRequestAccessor(mrk.ProjectKey)
            .GetSingleMergeRequestAccessor(mrk.IId)
            .GetDiscussionAccessor()
            .GetSingleDiscussionAccessor(discussionId)
            .GetDiscussionEditor();
      }

      private readonly GitLabInstance _gitLabInstance;
   }
}

