using System;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;
using mrHelper.Common.Types;

namespace mrHelper.Client.Workflow
{
   public class WorkflowState : IWorkflowState
   {
      public string HostName
      {
         get { return _hostName; }
         internal set
         {
            _hostName = value;
            _currentUser = default(User);
         }
      }

      public User CurrentUser
      {
         get { return _currentUser; }
         internal set { _currentUser = value; }
      }

      public MergeRequest MergeRequest
      {
         get { return _mergeRequest; }
         set
         {
            _mergeRequest = value;
         }
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
            };
         }
      }

      public MergeRequestKey MergeRequestKey
      {
         get
         {
            return new MergeRequestKey
            {
               ProjectKey = new ProjectKey { HostName = HostName, ProjectId = Project.Id },
               IId = MergeRequest.IId
            };
         }
      }

      internal Project Project
      {
         get
         {
            foreach (Project project in MergeRequests.Keys)
            {
               if (project.Id == MergeRequest.Project_Id)
               {
                  return project;
               }
            }

            return default(Project);
         }
      }

      internal Dictionary<Project, List<MergeRequest>> MergeRequests =
         new Dictionary<Project, List<MergeRequest>>();

      private string _hostName = String.Empty;
      private User _currentUser = new User();
      private MergeRequest _mergeRequest = new MergeRequest();
   }
}

