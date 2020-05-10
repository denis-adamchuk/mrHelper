using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Client.Common;
using mrHelper.Client.Types;

namespace mrHelper.Client.Workflow
{
   internal class VersionLoader : BaseWorkflowLoader, IVersionLoader
   {
      internal VersionLoader(WorkflowDataOperator op)
         : base(op)
      {
      }

      public INotifier<IVersionLoaderListener> GetNotifier() => _notifier;

      async public Task<bool> LoadCommitsAsync(MergeRequestKey mrk)
      {
         _notifier.OnPreLoadComparableEntities(mrk);
         IEnumerable<Commit> commits;
         try
         {
            commits = await _operator.GetCommitsAsync(mrk.ProjectKey.ProjectName, mrk.IId);
         }
         catch (OperatorException ex)
         {
            string cancelMessage = String.Format("Cancelled loading commits for merge request with IId {0}",
               mrk.IId);
            string errorMessage = String.Format("Cannot load commits for merge request with IId {0}",
               mrk.IId);
            handleOperatorException(ex, cancelMessage, errorMessage,
               new Action[] { new Action(() => _notifier.OnFailedLoadComparableEntities(mrk)) });
            return false;
         }
         _notifier.OnPostLoadComparableEntities(mrk, commits);
         return true;
      }

      async public Task<bool> LoadVersionsAsync(MergeRequestKey mrk, bool invokeCompareableEntitiesCallback)
      {
         List<Action> failureActions =
            new List<Action> { new Action(() => _notifier.OnFailedLoadVersions(mrk)) };
         if (invokeCompareableEntitiesCallback)
         {
            failureActions.Add(new Action(() => _notifier.OnFailedLoadComparableEntities(mrk)));
            _notifier.OnPreLoadComparableEntities(mrk);
         }
         _notifier.OnPreLoadVersions(mrk);

         IEnumerable<GitLabSharp.Entities.Version> versions;
         try
         {
            versions = await _operator.GetVersionsAsync(mrk.ProjectKey.ProjectName, mrk.IId);
         }
         catch (OperatorException ex)
         {
            string cancelMessage = String.Format("Cancelled loading versions for merge request with IId {0}",
               mrk.IId);
            string errorMessage = String.Format("Cannot load versions for merge request with IId {0}",
               mrk.IId);
            handleOperatorException(ex, cancelMessage, errorMessage, failureActions);
            return false;
         }

         if (invokeCompareableEntitiesCallback)
         {
            _notifier?.OnPostLoadComparableEntities(mrk, versions);
         }
         _notifier?.OnPostLoadVersions(mrk, versions);

         return true;
      }

      private readonly VersionLoaderNotifier _notifier = new VersionLoaderNotifier();
   }
}

