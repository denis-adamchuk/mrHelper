using mrHelper.Client.Common;
using System.Threading.Tasks;

namespace mrHelper.Client.Workflow
{
   internal interface IWorkflowLoader : ILoader<IWorkflowEventListener>
   {
      Task<bool> Load(string hostname, IWorkflowContext context);
   }
}

