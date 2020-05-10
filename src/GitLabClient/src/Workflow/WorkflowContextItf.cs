using System.Collections.Generic;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.Workflow
{
   public interface IWorkflowContext { }

   public class ProjectBasedContext : IWorkflowContext
   {
      public IEnumerable<ProjectKey> Projects;
   }

   public class LabelBasedContext : IWorkflowContext
   {
      public object Search;
   }
}

