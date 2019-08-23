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
            _currentUser = default(User);
         }
      }

      public User CurrentUser
      {
         get { return _currentUser; }
         set { _currentUser = value; }
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
            _commits = null;
            _systemSystemNotes = null;
         }
      }

      public List<Commit> Commits
      {
         get { return _commits; }
         set { _commits = value; }
      }

      public List<Note> SystemNotes
      {
         get { return _systemSystemNotes; }
         set { _systemSystemNotes = value; }
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
      private List<Project> _projects = new List<Project>();
      private Project _project = new Project();
      private List<MergeRequest> _mergeRequests = new List<MergeRequest>();
      private MergeRequest _mergeRequest = new MergeRequest();
      private List<Commit> _commits = new List<Commit>();
      private List<Note> _systemSystemNotes = new List<Note>();
   }
}

