using mrHelper.Client.Common;
using System.Threading.Tasks;

namespace mrHelper.Client.Workflow
{
   internal interface IMergeRequestListLoader : ILoader<IMergeRequestListLoaderListener>
   {
      Task<bool> Load(IWorkflowContext context);
   }
}

