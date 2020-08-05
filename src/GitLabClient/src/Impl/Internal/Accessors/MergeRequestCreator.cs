using System.Threading.Tasks;
using GitLabSharp.Accessors;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient.Operators;

namespace mrHelper.GitLabClient.Accessors
{
   public class MergeRequestCreator : IMergeRequestCreator
   {
      internal MergeRequestCreator(ProjectKey projectKey, IHostProperties hostProperties)
      {
         _projectKey = projectKey;
         _hostProperties = hostProperties;
      }

      async public Task<MergeRequest> CreateMergeRequest(CreateNewMergeRequestParameters parameters)
      {
         using (MergeRequestOperator mergeRequestOperator = new MergeRequestOperator(
            _projectKey.HostName, _hostProperties))
         {
            try
            {
               return await mergeRequestOperator.CreateMergeRequest(_projectKey.ProjectName, parameters);
            }
            catch (OperatorException)
            {
               // TODO WTF Need to wrap into another exception type
               return null;
            }
         }
      }

      private readonly ProjectKey _projectKey;
      private readonly IHostProperties _hostProperties;
   }
}

