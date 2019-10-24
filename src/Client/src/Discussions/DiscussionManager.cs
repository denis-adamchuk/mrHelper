using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;
using mrHelper.Client.Discussions;
using mrHelper.Common.Exceptions;

namespace mrHelper.Client.Discussions
{
   public class DiscussionManagerException : Exception {}

   /// <summary>
   /// Manages merge request discussions
   /// </summary>
   public class DiscussionManager
   {
      public event Action<MergeRequestKey> PreLoadDiscussions;
      public event Action<MergeRequestKey, List<Discussion>> PostLoadDiscussions;

      public DiscussionManager(UserDefinedSettings settings, Workflow workflow)
      {
         Settings = settings;
         DiscussionOperator = new DiscussionOperator(settings);
         workflow.PostLoadProjectMergeRequests +=
            (hostname, project, mergeRequests) => requestDiscussions(hostname, project, mergeRequests);
      }

      async public Task<List<Discussion>> GetDiscussionsAsync(MergeRequestKey mrk)
      {
         while (Updating.ContainsKey(mrk))
         {
            await Task.Delay(50);
         }

         await updateDiscussionsAsync(mrk);
         return Discussions[mrk];
      }

      public DiscussionCreator GetDiscussionCreator(MergeRequestKey mrk)
      {
         return new DiscussionCreator(mrk, DiscussionOperator);
      }

      public DiscussionEditor GetDiscussionEditor(MergeRequestKey mrk, string discussionId)
      {
         return new DiscussionEditor(mrk, discussionId, DiscussionOperator);
      }

      private void requestDiscussions(string hostname, Project project, List<MergeRequest> mergeRequests)
      {
         foreach (MergeRequest mergeRequest in mergeRequests)
         {
            MergeRequestKey mrk = new MergeRequestKey
            {
               HostName = hostname,
               ProjectName = project.Path_With_Namespace,
               IId = mergeRequest.IId
            };
            BeginInvoke(new Action(async () => await updateDiscussionsAsync(mrk)), null);
         }
      }

      async private void getMergeRequestUpdateTimeStampAsync(MergeRequestKey mrk)
      {
         GitLabClient client = new GitLabClient(mrk.ProjectKey.HostName, Settings.GetAccessToken(mrk.ProjectKey.HostName));
         MergeRequest mergeRequest = await CommonOperator.GetMergeRequestAsync(client, mrk.ProjectKey.ProjectName, mrk.IId);
         return mergeRequest.Updated_At;
      }

      async private void updateDiscussionsAsync(MergeRequestKey mrk)
      {
         if (UpdateTimeStamp.ContainsKey(mrk) && await getMergeRequestUpdateTimeStamp(mrk) <= UpdateTimeStamp[mrk])
         {
            return;
         }

         try
         {
            PreLoadDiscussions?.Invoke(mrk);
            Updating[mrk] = new GitLabClient(mrk.ProjectKey.HostName, Settings.GetAccessToken(mrk.ProjectKey.HostName));
            Discussions[mrk] = await DiscussionOperator.GetDiscussionsAsync(Updating[mrk], mrk);
            UpdateTimeStamp[mrk] = await getMergeRequestUpdateTimeStamp(mrk);
            PostLoadDiscussions?.Invoke(mrk, Discussions[mrk]);
         }
         catch (OperatorException)
         {
            throw new DiscussionManagerException();
         }
         finally
         {
            Updating.Remove(mrk);
         }
      }

      private UserDefinedSettings Settings { get; }
      private DiscussionOperator DiscussionOperator { get; }
      private Dictionary<MergeRequestKey, DateTime> UpdateTimeStamp { get; }
      private Dictionary<MergeRequestKey, List<Discussion>> Discussions { get; }
      private Dictionary<MergeRequestKey, GitLabClient> Updating { get; }
   }
}

