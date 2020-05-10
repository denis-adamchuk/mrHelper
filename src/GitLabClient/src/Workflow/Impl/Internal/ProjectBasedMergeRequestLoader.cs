using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitLabSharp.Accessors;
using GitLabSharp.Entities;
using mrHelper.Client.Common;
using mrHelper.Client.Types;
using mrHelper.Common.Constants;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;

namespace mrHelper.Client.Workflow
{
   internal class ProjectBasedMergeRequestLoader : BaseWorkflowLoader, IMergeRequestListLoader
   {
      public ProjectBasedMergeRequestLoader(
         GitLabClientContext clientContext, WorkflowDataOperator op, IVersionLoader versionLoader)
         : base(op)
      {
         _versionLoader = versionLoader;
         _onForbiddenProject = clientContext.OnForbiddenProject;
         _onNotFoundProject = clientContext.OnNotFoundProject;
      }

      public INotifier<IMergeRequestListLoaderListener> GetNotifier() => _notifier;

      async public Task<bool> Load(IWorkflowContext context)
      {
         Exception exception = null;
         bool cancelled = false;
         async Task loadVersionsLocal(MergeRequestKey mrk)
         {
            if (cancelled)
            {
               return;
            }

            try
            {
               if (!await _versionLoader.LoadVersionsAsync(mrk, false))
               {
                  cancelled = true;
               }
            }
            catch (WorkflowException ex)
            {
               exception = ex;
               cancelled = true;
            }
         }

         async Task loadProject(ProjectKey project)
         {
            if (cancelled)
            {
               return;
            }

            try
            {
               IEnumerable<MergeRequest> mergeRequests = await loadProjectMergeRequestsAsync(project);
               if (mergeRequests == null)
               {
                  cancelled = true;
               }
               else
               {
                  await TaskUtils.RunConcurrentFunctionsAsync(mergeRequests,
                     x => loadVersionsLocal(new MergeRequestKey { IId = x.IId, ProjectKey = project }),
                     Constants.MergeRequestsInBatch, Constants.MergeRequestsInterBatchDelay, () => cancelled);
               }
            }
            catch (WorkflowException ex)
            {
               if (isForbiddenProjectException(ex))
               {
                  _onForbiddenProject?.Invoke(project);
                  return;
               }
               else if (isNotFoundProjectException(ex))
               {
                  _onNotFoundProject?.Invoke(project);
                  return;
               }
               exception = ex;
               cancelled = true;
            }
         }

         await TaskUtils.RunConcurrentFunctionsAsync(
            (context as ProjectBasedContext).Projects, x => loadProject(x),
            Constants.ProjectsInBatch, Constants.ProjectsInterBatchDelay, () => cancelled);
         if (!cancelled)
         {
            return true;
         }

         if (exception != null)
         {
            throw exception;
         }
         return false;
      }

      async private Task<IEnumerable<MergeRequest>> loadProjectMergeRequestsAsync(ProjectKey project)
      {
         _notifier.OnPreLoadProjectMergeRequests(project);

         string projectName = project.ProjectName;

         IEnumerable<MergeRequest> mergeRequests;
         try
         {
            SearchByProject searchByProject = new SearchByProject { ProjectName = projectName };
            mergeRequests = await _operator.SearchMergeRequestsAsync(searchByProject, null, true);
         }
         catch (OperatorException ex)
         {
            string cancelMessage = String.Format("Cancelled loading merge requests for project \"{0}\"", projectName);
            string errorMessage = String.Format("Cannot load project \"{0}\"", projectName);
            handleOperatorException(ex, cancelMessage, errorMessage,
               new Action[] { new Action(() => _notifier.OnFailedLoadProjectMergeRequests(project)) });
            return null;
         }

         _notifier.OnPostLoadProjectMergeRequests(project, mergeRequests);
         return mergeRequests;
      }

      private static bool isForbiddenProjectException(WorkflowException ex)
      {
         System.Net.HttpWebResponse response = getWebResponse(ex);
         return response != null ? response.StatusCode == System.Net.HttpStatusCode.Forbidden : false;
      }

      private static bool isNotFoundProjectException(WorkflowException ex)
      {
         System.Net.HttpWebResponse response = getWebResponse(ex);
         return response != null ? response.StatusCode == System.Net.HttpStatusCode.NotFound : false;
      }

      private static System.Net.HttpWebResponse getWebResponse(WorkflowException ex)
      {
         if (ex.InnerException?.InnerException is GitLabRequestException rx)
         {
            if (rx.InnerException is System.Net.WebException wx)
            {
               System.Net.HttpWebResponse response = wx.Response as System.Net.HttpWebResponse;
               return response;
            }
         }
         return null;
      }

      private readonly IVersionLoader _versionLoader;
      private readonly MergeRequestListLoaderNotifier _notifier = new MergeRequestListLoaderNotifier();
      private readonly Action<ProjectKey> _onForbiddenProject;
      private readonly Action<ProjectKey> _onNotFoundProject;
   }
}

