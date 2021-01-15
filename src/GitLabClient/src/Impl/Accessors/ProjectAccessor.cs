using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient.Operators;

namespace mrHelper.GitLabClient
{
   public class ProjectAccessor
   {
      internal ProjectAccessor(IHostProperties settings, string hostname,
         IModificationListener modificationListener, INetworkOperationStatusListener networkOperationStatusListener)
      {
         _settings = settings;
         _hostname = hostname;
         _modificationListener = modificationListener;
         _networkOperationStatusListener = networkOperationStatusListener;
      }

      async public Task<Project> SearchProjectAsync(string projectname)
      {
         using (ProjectOperator projectOperator = new ProjectOperator(_hostname, _settings, _networkOperationStatusListener))
         {
            try
            {
               return await projectOperator.SearchProjectAsync(projectname);
            }
            catch (OperatorException)
            {
               return null;
            }
         }
      }

      public SingleProjectAccessor GetSingleProjectAccessor(string projectName)
      {
         return new SingleProjectAccessor(new ProjectKey(_hostname, projectName),
            _settings, _modificationListener, _networkOperationStatusListener);
      }

      private readonly IHostProperties _settings;
      private readonly string _hostname;
      private readonly IModificationListener _modificationListener;
      private readonly INetworkOperationStatusListener _networkOperationStatusListener;
   }
}

