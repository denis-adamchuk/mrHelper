using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient;

namespace mrHelper.App.Helpers.GitLab
{
   // TODO WTF Add checks on gitLabInstance
   internal static class Shortcuts
   {
      internal static ProjectAccessor GetProjectAccessor(GitLabInstance gitLabInstance)
      {
         throwOnNullInstance(gitLabInstance);
         return new RawDataAccessor(gitLabInstance)
            .ProjectAccessor;
      }

      internal static UserAccessor GetUserAccessor(GitLabInstance gitLabInstance)
      {
         throwOnNullInstance(gitLabInstance);
         return new RawDataAccessor(gitLabInstance)
            .UserAccessor;
      }

      internal static MergeRequestAccessor GetMergeRequestAccessor(GitLabInstance gitLabInstance,
         ProjectKey projectKey)
      {
         throwOnNullInstance(gitLabInstance);
         return new RawDataAccessor(gitLabInstance)
            .ProjectAccessor
            .GetSingleProjectAccessor(projectKey.ProjectName)
            .MergeRequestAccessor;
      }

      internal static IMergeRequestCreator GetMergeRequestCreator(GitLabInstance gitLabInstance,
         ProjectKey projectKey)
      {
         throwOnNullInstance(gitLabInstance);
         return GetMergeRequestAccessor(gitLabInstance, projectKey)
            .GetMergeRequestCreator();
      }

      internal static IMergeRequestEditor GetMergeRequestEditor(GitLabInstance gitLabInstance,
         MergeRequestKey mrk)
      {
         throwOnNullInstance(gitLabInstance);
         return GetMergeRequestAccessor(gitLabInstance, mrk.ProjectKey)
            .GetSingleMergeRequestAccessor(mrk.IId)
            .GetMergeRequestEditor();
      }

      internal static ITimeTracker GetTimeTracker(GitLabInstance gitLabInstance, MergeRequestKey mrk)
      {
         throwOnNullInstance(gitLabInstance);
         return GetMergeRequestAccessor(gitLabInstance, mrk.ProjectKey)
            .GetSingleMergeRequestAccessor(mrk.IId).GetTimeTracker();
      }

      internal static IDiscussionCreator GetDiscussionCreator(GitLabInstance gitLabInstance,
         MergeRequestKey mrk, User user)
      {
         throwOnNullInstance(gitLabInstance);
         return GetMergeRequestAccessor(gitLabInstance, mrk.ProjectKey)
            .GetSingleMergeRequestAccessor(mrk.IId)
            .GetDiscussionAccessor()
            .GetDiscussionCreator(user);
      }

      internal static SingleDiscussionAccessor GetSingleDiscussionAccessor(GitLabInstance gitLabInstance,
         MergeRequestKey mrk, string discussionId)
      {
         throwOnNullInstance(gitLabInstance);
         return GetMergeRequestAccessor(gitLabInstance, mrk.ProjectKey)
            .GetSingleMergeRequestAccessor(mrk.IId)
            .GetDiscussionAccessor()
            .GetSingleDiscussionAccessor(discussionId);
      }

      private static void throwOnNullInstance(GitLabInstance gitLabInstance)
      {
         if (gitLabInstance == null)
         {
            throw new System.ArgumentException("gitLabInstance argument cannot be null");
         }
      }
   }
}

