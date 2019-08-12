using System;
using mrHelper.Client.Tools;

namespace mrHelper.Client.Workflow;
{
   public class WorkflowState
   {
      public string HostName
      {
         get;
         set
         {
            HostName = value;
            Projects = null;
         }
      }

      public List<Project> Projects
      {
         get;
         set
         {
            Projects = value;
            Project = null;
         }
      }

      public Project Project
      {
         get;
         set
         {
            Project = value;
            MergeRequests = null;
         }
      }

      public List<MergeRequest> MergeRequests
      {
         get;
         set
         {
            MergeRequests = value;
            MergeRequest = null
         }
      }

      public MergeRequest MergeRequest
      {
         get;
         set
         {
            MergeRequest = value;
            Versions = null;
         }
      }

      public List<Version> Versions
      {
         get;
         set
      }

      public MergeRequestDescriptor MergeRequestDescriptor
      {
         get
         {
            return new MergeRequestDescriptor
            {
               HostName = HostName,
               ProjectName = Project.Path_With_Namespace,
               IId = MergeRequest.IId
            }
      }
   }
}

