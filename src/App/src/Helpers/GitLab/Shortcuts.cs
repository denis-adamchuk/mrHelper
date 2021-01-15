using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient;

namespace mrHelper.App.Helpers.GitLab
{
   internal class Shortcuts
   {
      internal Shortcuts(GitLabInstance gitLabIntance)
      {
         _gitLabInstance = gitLabIntance;
      }

      internal ProjectAccessor GetProjectAccessor()
      {
         return new RawDataAccessor(_gitLabInstance)
            .GetProjectAccessor();
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

