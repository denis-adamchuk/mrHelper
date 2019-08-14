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
            _projects = null;
         }
      }

      public List<Project> Projects
      {
         get { return _projects; }
         set
         {
            _projects = value;
            _project = default(Project);
         }
      }

      public Project Project
      {
         get { return _project; }
         set
         {
            _project = value;
            _mergeRequests = null;
         }
      }

      public List<MergeRequest> MergeRequests
      {
         get { return _mergeRequests; }
         set
         {
            _mergeRequests = value;
            _mergeRequest = default(MergeRequest);
         }
      }

      public MergeRequest MergeRequest
      {
         get { return _mergeRequest; }
         set
         {
            _mergeRequest = value;
            _versions = null;
         }
      }

      public List<GitLabSharp.Entities.Version> Versions
      {
         get { return _versions; }
         set { _versions = value; }
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
      private List<Project> _projects = new List<Project>();
      private Project _project = new Project();
      private List<MergeRequest> _mergeRequests = new List<MergeRequest>();
      private MergeRequest _mergeRequest = new MergeRequest();
      private List<GitLabSharp.Entities.Version> _versions = new List<GitLabSharp.Entities.Version>();
   }
}

