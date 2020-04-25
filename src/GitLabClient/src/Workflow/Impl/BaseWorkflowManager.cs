using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp;
using GitLabSharp.Entities;
using GitLabSharp.Accessors;
using Version = GitLabSharp.Entities.Version;
using mrHelper.Client.Types;
using mrHelper.Client.Common;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;

namespace mrHelper.Client.Workflow
{
   public class WorkflowException : ExceptionEx
   {
      internal WorkflowException(string message, Exception innerException)
         : base(message, innerException) {}

      public string UserMessage
      {
         get
         {
            if (InnerException is OperatorException ox)
            {
               if (ox.InnerException is GitLabRequestException rx)
               {
                  if (rx.InnerException is System.Net.WebException wx)
                  {
                     System.Net.HttpWebResponse response = wx.Response as System.Net.HttpWebResponse;
                     if (response != null && response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                     {
                        return wx.Message + " Check your access token!";
                     }
                     return wx.Message;
                  }
               }
            }
            return OriginalMessage;
         }
      }
   }

   /// <summary>
   /// Supports chains of actions (loading a merge request also loads its versions or commits)
   /// Each action toggles Pre-{Action}-Event and either Post-{Action}-Event or Failed-{Action}-Event
   /// </summary>
   public class BaseWorkflowManager : IMergeRequestLoader
   {
      internal BaseWorkflowManager(IHostProperties settings)
      {
         _settings = settings;
      }

      async public Task<bool> LoadMergeRequestAsync(string hostname, string projectname, int mergeRequestIId,
         EComparableEntityType comparableEntityType)
      {
         _operator = new WorkflowDataOperator(hostname, _settings.GetAccessToken(hostname));

         return await loadMergeRequestAsync(hostname, projectname, mergeRequestIId, comparableEntityType);
      }

      async public Task CancelAsync()
      {
         if (_operator != null)
         {
            await _operator.CancelAsync();
         }
      }

      public event Action<int> PreLoadMergeRequest;
      public event Action<string, string, MergeRequest> PostLoadMergeRequest;
      public event Action FailedLoadMergeRequest;

      public event Action PreLoadComparableEntities;
      public event Action<string, string, MergeRequest, System.Collections.IEnumerable> PostLoadComparableEntities;
      public event Action FailedLoadComparableEntities;

      public event Action PreLoadVersions;
      public event Action<string, string, MergeRequest, IEnumerable<Version>> PostLoadVersions;
      public event Action FailedLoadVersions;

      async private Task<bool> loadMergeRequestAsync(string hostname, string projectName, int mergeRequestIId,
         EComparableEntityType comparableEntityType)
      {
         PreLoadMergeRequest?.Invoke(mergeRequestIId);

         MergeRequest mergeRequest = new MergeRequest();
         try
         {
            SearchByIId searchByIId = new SearchByIId { ProjectName = projectName, IId = mergeRequestIId };
            IEnumerable<MergeRequest> mergeRequests = await _operator.SearchMergeRequestsAsync(searchByIId, null, true);
            mergeRequest = mergeRequests.FirstOrDefault();
         }
         catch (OperatorException ex)
         {
            string cancelMessage = String.Format("Cancelled loading MR with IId {0}", mergeRequestIId);
            string errorMessage = String.Format("Cannot load merge request with IId {0}", mergeRequestIId);
            handleOperatorException(ex, cancelMessage, errorMessage, new Action[] { FailedLoadMergeRequest });
            return false;
         }

         PostLoadMergeRequest?.Invoke(hostname, projectName, mergeRequest);

         switch (comparableEntityType)
         {
            case EComparableEntityType.Commit:
               return await loadVersionsAsync(hostname, projectName, mergeRequest, false)
                   && await loadCommitsAsync(hostname, projectName, mergeRequest);

            case EComparableEntityType.Version:
               return await loadVersionsAsync(hostname, projectName, mergeRequest, true);
         }

         Debug.Assert(false);
         return true;
      }

      async private Task<bool> loadCommitsAsync(string hostname, string projectName, MergeRequest mergeRequest)
      {
         PreLoadComparableEntities?.Invoke();
         IEnumerable<Commit> commits;
         try
         {
            commits = await _operator.GetCommitsAsync(projectName, mergeRequest.IId);
         }
         catch (OperatorException ex)
         {
            string cancelMessage = String.Format("Cancelled loading commits for merge request with IId {0}",
               mergeRequest.IId);
            string errorMessage = String.Format("Cannot load commits for merge request with IId {0}",
               mergeRequest.IId);
            handleOperatorException(ex, cancelMessage, errorMessage, new Action[] { FailedLoadComparableEntities });
            return false;
         }
         PostLoadComparableEntities?.Invoke(hostname, projectName, mergeRequest, commits);
         return true;
      }

      async protected Task<bool> loadVersionsAsync(string hostname, string projectName, MergeRequest mergeRequest,
         bool invokeCompareableEntitiesCallback)
      {
         List<Action> failureActions = new List<Action> { FailedLoadVersions };
         if (invokeCompareableEntitiesCallback)
         {
            failureActions.Add(FailedLoadComparableEntities);
            PreLoadComparableEntities?.Invoke();
         }
         PreLoadVersions?.Invoke();

         IEnumerable<Version> versions;
         try
         {
            versions = await _operator.GetVersionsAsync(projectName, mergeRequest.IId);
         }
         catch (OperatorException ex)
         {
            string cancelMessage = String.Format("Cancelled loading versions for merge request with IId {0}",
               mergeRequest.IId);
            string errorMessage = String.Format("Cannot load versions for merge request with IId {0}",
               mergeRequest.IId);
            handleOperatorException(ex, cancelMessage, errorMessage, failureActions);
            return false;
         }

         if (invokeCompareableEntitiesCallback)
         {
            PostLoadComparableEntities?.Invoke(hostname, projectName, mergeRequest, versions);
         }
         PostLoadVersions?.Invoke(hostname, projectName, mergeRequest, versions);

         return true;
      }

      internal void handleOperatorException(OperatorException ex, string cancelMessage, string errorMessage,
         IEnumerable<Action> failureActions)
      {
         bool cancelled = ex.InnerException is GitLabClientCancelled;
         if (cancelled)
         {
            Trace.TraceInformation(String.Format("[WorkflowManager] {0}", cancelMessage));
            return;
         }

         failureActions?.ToList().ForEach(x => x?.Invoke());

         throw new WorkflowException(errorMessage, ex);
      }

      protected readonly IHostProperties _settings;
      internal WorkflowDataOperator _operator;
   }
}

