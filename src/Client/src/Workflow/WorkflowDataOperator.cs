using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp;
using GitLabSharp.Accessors;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;

namespace mrHelper.Client.Workflow
{
   /// <summary>
   /// Implements Workflow-related interaction with GitLab
   /// </summary>
   internal class WorkflowDataOperator : IDisposable
   {
      internal WorkflowDataOperator(string host, string token)
      {
         Client = new GitLabClient(host, token);
      }

      public void Dispose()
      {
         Client.Dispose();
      }

      async internal Task<User> GetCurrentUserAsync()
      {
         try
         {
            return (User)(await Client.RunAsync(async (gl) => await gl.CurrentUser.LoadTaskAsync() ));
         }
         catch (Exception ex)
         {
            if (ex is GitLabSharpException || ex is GitLabRequestException || ex is GitLabClientCancelled)
            {
               ExceptionHandlers.Handle(ex, "Cannot load current user from GitLab");
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      async internal Task<List<Project>> GetProjectsAsync(string hostName, bool publicOnly)
      {
         List<Project> projects = Tools.Tools.LoadProjectsFromFile(hostName);
         if (projects != null && projects.Count != 0)
         {
            await Client.CancelAsync();
            return projects;
         }

         try
         {
            return (List<Project>)(await Client.RunAsync(async (gl) =>
               await gl.Projects.LoadAllTaskAsync(new ProjectsFilter { PublicOnly = publicOnly })));
         }
         catch (Exception ex)
         {
            if (ex is GitLabSharpException || ex is GitLabRequestException || ex is GitLabClientCancelled)
            {
               ExceptionHandlers.Handle(ex, "Cannot load projects from GitLab");
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      async internal Task<Project> GetProjectAsync(string projectName)
      {
         try
         {
            return (Project)(await Client.RunAsync(async (gl) => await gl.Projects.Get(projectName).LoadTaskAsync()));
         }
         catch (Exception ex)
         {
            if (ex is GitLabSharpException || ex is GitLabRequestException || ex is GitLabClientCancelled)
            {
               ExceptionHandlers.Handle(ex, "Cannot load project from GitLab");
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      async internal Task<List<MergeRequest>> GetMergeRequestsAsync(string projectName, List<string> labels)
      {
         List<MergeRequest> mergeRequests = null;
         try
         {
            mergeRequests = (List<MergeRequest>)(await Client.RunAsync(async (gl) =>
               await gl.Projects.Get(projectName).MergeRequests.LoadAllTaskAsync(
                  new MergeRequestsFilter())));
         }
         catch (Exception ex)
         {
            if (ex is GitLabSharpException || ex is GitLabRequestException || ex is GitLabClientCancelled)
            {
               ExceptionHandlers.Handle(ex, "Cannot load merge requests from GitLab");
               throw new OperatorException(ex);
            }
            throw;
         }

         for (int iMergeRequest = mergeRequests.Count - 1; iMergeRequest >= 0; --iMergeRequest)
         {
            if (labels != null && labels.Intersect(mergeRequests[iMergeRequest].Labels).Count() == 0)
            {
               mergeRequests.RemoveAt(iMergeRequest);
            }
         }

         return mergeRequests;
      }

      async internal Task<MergeRequest> GetMergeRequestAsync(string projectName, int iid)
      {
         try
         {
            return (MergeRequest)(await Client.RunAsync(async (gl) =>
               await gl.Projects.Get(projectName).MergeRequests.Get(iid).LoadTaskAsync()));
         }
         catch (Exception ex)
         {
            if (ex is GitLabSharpException || ex is GitLabRequestException || ex is GitLabClientCancelled)
            {
               ExceptionHandlers.Handle(ex, "Cannot load merge request from GitLab");
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      async internal Task<List<Commit>> GetCommitsAsync(string projectName, int iid)
      {
         try
         {
            return (List<Commit>)(await Client.RunAsync(async (gl) =>
               await gl.Projects.Get(projectName).MergeRequests.Get(iid).Commits.LoadAllTaskAsync()));
         }
         catch (Exception ex)
         {
            if (ex is GitLabSharpException || ex is GitLabRequestException || ex is GitLabClientCancelled)
            {
               ExceptionHandlers.Handle(ex, "Cannot load merge request commits from GitLab");
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      async internal Task<List<Note>> GetSystemNotesAsync(string projectName, int iid)
      {
         List<Note> allNotes;
         try
         {
            allNotes = (List<Note>)(await Client.RunAsync(async (gl) =>
               await gl.Projects.Get(projectName).MergeRequests.Get(iid).Notes.LoadAllTaskAsync()));
         }
         catch (Exception ex)
         {
            if (ex is GitLabSharpException || ex is GitLabRequestException || ex is GitLabClientCancelled)
            {
               ExceptionHandlers.Handle(ex, "Cannot load merge request notes from GitLab");
               throw new OperatorException(ex);
            }
            throw;
         }
         return allNotes.Where((x) => x.System == true).ToList();
      }

      public Task CancelAsync()
      {
         return Client.CancelAsync();
      }

      private GitLabClient Client { get; }
   }
}

