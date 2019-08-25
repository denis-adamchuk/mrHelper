using System;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;

namespace mrHelper.Client.Workflow
{
   public class WorkflowState
   {
      public string HostName
      {
         get { return _hostName; }
         set
         {
            _hostName = value;
            _currentUser = default(User);
         }
      }

      public User CurrentUser
      {
         get { return _currentUser; }
         set { _currentUser = value; }
      }

      public Project Project
      {
         get { return _project; }
         set
         {
            _project = value;
         }
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

      private string _hostName = String.Empty;
      private User _currentUser = new User();
      private Project _project = new Project();
      private MergeRequest _mergeRequest = new MergeRequest();
   }
}

