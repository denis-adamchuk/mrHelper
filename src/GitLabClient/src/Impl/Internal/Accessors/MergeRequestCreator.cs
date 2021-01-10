using System.Threading.Tasks;
using GitLabSharp.Accessors;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient.Interfaces;
using mrHelper.GitLabClient.Operators;

namespace mrHelper.GitLabClient.Accessors
{
   public class MergeRequestCreator : IMergeRequestCreator
   {
      internal MergeRequestCreator(ProjectKey projectKey, IHostProperties hostProperties,
         IConnectionLossListener connectionLossListener)
      {
         _projectKey = projectKey;
         _hostProperties = hostProperties;
         _connectionLossListener = connectionLossListener;
      }

      async public Task<MergeRequest> CreateMergeRequest(CreateNewMergeRequestParameters parameters)
      {
         using (MergeRequestOperator mergeRequestOperator = new MergeRequestOperator(
            _projectKey.HostName, _hostProperties, _connectionLossListener))
         {
            try
            {
               return await mergeRequestOperator.CreateMergeRequest(_projectKey.ProjectName, parameters);
            }
            catch (OperatorException ex)
            {
               if (ex.Cancelled)
               {
                  throw new MergeRequestCreatorCancelledException();
               }
               if (ex.InnerException is GitLabRequestException glx)
               {
                  throw new MergeRequestCreatorException("Cannot create MR", glx);
               }
               throw new MergeRequestCreatorException("Cannot create MR by unknown reason", null);
            }
         }
      }

      private readonly ProjectKey _projectKey;
      private readonly IHostProperties _hostProperties;
      private readonly IConnectionLossListener _connectionLossListener;
   }
}

