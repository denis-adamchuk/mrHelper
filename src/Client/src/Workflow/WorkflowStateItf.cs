using GitLabSharp.Entities;
using mrHelper.Client.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrHelper.Client.Workflow
{
   public interface IWorkflowState
   {
      string HostName { get; }
      User CurrentUser { get; }
      MergeRequest MergeRequest { get; }
      MergeRequestDescriptor MergeRequestDescriptor { get; }
      MergeRequestKey MergeRequestKey { get; }
   }
}
