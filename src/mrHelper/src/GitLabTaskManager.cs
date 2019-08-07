using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace mrHelperUI
{
   /// <summary>
   /// Types of asynchronous requests that we made to GitLab
   /// </summary>
   public enum GitLabTaskType
   {
      Projects,
      MergeRequests,
      MergeRequest,
      Versions,
      Discussions,
      CurrentUser
      // TODO Add other requests here as well
   }

   /// <summary>
   /// Manages asynchronous requests that UI makes to GitLab
   /// </summary>
   public class GitLabTaskManager
   {
      /// <summary>
      /// Describes a request to GitLab
      /// </summary>
      public interface IGitLabTask
      {
         bool IsCancelled();
         void Cancel();
         GitLabTaskType GetTaskType();
         Task WaitAsync();
      }

      /// <summary>
      /// Implements IGitLabTask with specific result type
      /// </summary>
      public class GitLabTask<TResult> : IGitLabTask
      {
         /// <summary>
         /// 
         /// </summary>
         public GitLabTask(Task<TResult> task, GitLabTaskType type)
         {
            Task = task;
            Type = type;
         }

         /// <summary>
         /// Returns a thing that can be 'await'-ed
         /// </summary>
         public TaskAwaiter<TResult> GetAwaiter()
         {
            return Task.GetAwaiter();
         }

         /// <summary>
         /// Checks if this task is cancelled
         /// </summary>
         public bool IsCancelled()
         {
            return Cancelled;
         }

         /// <summary>
         /// Marks the task as cancelled
         /// </summary>
         public void Cancel()
         {
            Cancelled = true;
         }

         /// <summary>
         /// Returns type of task
         /// </summary>
         public GitLabTaskType GetTaskType()
         {
            return Type;
         }

         /// <summary>
         /// Allows to wait for task completion asynchronously, ignoring return value
         /// </summary>
         async public Task WaitAsync()
         {
            await Task;
         }

         private bool Cancelled { get; set; }
         private Task<TResult> Task { get; }
         private GitLabTaskType Type { get; }
      }

      /// <summary>
      /// Creates a GitLab task
      /// </summary>
      public GitLabTask<TResult> CreateTask<TResult>(Task<TResult> task, GitLabTaskType type)
      {
         var gitLabTask = new GitLabTask<TResult>(task, type);
         cancelByType(type);
         _RunningTasks.Add(gitLabTask);
         return gitLabTask;
      }

      /// <summary>
      /// Schedules the task for asynchronous execution and when it finishes, returns the result
      /// </summary>
      async public Task<TResult> RunAsync<TResult>(GitLabTask<TResult> gitLabTask) where TResult : new()
      {
         TResult taskResult = await gitLabTask;
         _RunningTasks.Remove(gitLabTask);
         return gitLabTask.IsCancelled() ? default(TResult) : taskResult;
      }

      /// <summary>
      /// Cancels all running tasks in accordance with 'type map'
      /// Note that it is a kind of 'soft cancelling', tasks continue execution but result value is ignored
      /// </summary>
      public void CancelAll(GitLabTaskType type)
      {
         cancelByType(type);
      }

      /// <summary>
      /// Checks if there are any running tasks
      /// </summary>
      public bool AreRunningTasks()
      {
         return _RunningTasks.Count > 0;
      }

      /// <summary>
      /// Schedules tasks for asynchronous execution one-by-one but discards the return values.
      /// It is useful when user wants to close the App but some tasks are still running 
      /// Throws:
      /// GitLabRequestException
      /// </summary>
      async public Task WaitAllAsync()
      {
         Debug.WriteLine("Start waiting for task completion, count = " + _RunningTasks.Count);
         while (AreRunningTasks())
         {
            try
            {
               await _RunningTasks[0].WaitAsync();
            }
            catch (Exception ex)
            {
               Debug.Assert(ex is GitLabSharp.GitLabRequestException);
               _RunningTasks.RemoveAt(0);
               throw;
            }
            Debug.WriteLine("One more task has completed, remaining count = " + _RunningTasks.Count);
         }
      }

      /// <summary>
      /// Create a new cancellation token source and cancel ongoing asynchronous tasks. This is needed by two reasons:
      /// 1. Application UI state consistency. Consider next sequence:
      ///    - User switches to Project 1 in projects drop down list
      ///    - App initiates an async task #1 for all merge requests of Project 1 and suspends on await
      ///    - User switches to Project 2 in projects drop down list
      ///    - App initiates an async task #2 for all merge requests of Project 2 and suspends on await
      ///    - Async task #1 finishes and then the App initiates an async task #3 for specific merge request #1 details
      ///      At this moment selected project is Project 2 and App attempts to request merge request #1 for Project 2 and fails
      ///      because merge request #1 belongs to Project 1
      ///    We can divide all tasks in three groups:
      ///    - Cannot be started because of UI restrictions (selecting index -1 during GitLab request)
      ///    - Can be started without risks
      ///    - Cancel other tasks when started
      /// 2. TODO - Asynchronous HTTP requests can be cancelled immediately
      /// </summary>
      private void cancelByType(GitLabTaskType newTaskType)
      {
         for (int iTask = _RunningTasks.Count - 1; iTask >= 0; --iTask)
         {
            IGitLabTask oldTask = _RunningTasks[iTask];
            if (oldTask.IsCancelled())
            {
               continue;
            }

            GitLabTaskType oldTaskType = oldTask.GetTaskType();
            if (oldTaskType == GitLabTaskType.Projects)
            {
               Debug.Assert(newTaskType == GitLabTaskType.Projects);
               oldTask.Cancel();
            }
            else if (oldTaskType == GitLabTaskType.MergeRequests)
            {
               Debug.Assert(newTaskType == GitLabTaskType.Projects
                         || newTaskType == GitLabTaskType.MergeRequests);
               oldTask.Cancel();
            }
            else if (oldTaskType == GitLabTaskType.MergeRequest)
            {
               Debug.Assert(newTaskType == GitLabTaskType.Projects
                         || newTaskType == GitLabTaskType.MergeRequests
                         || newTaskType == GitLabTaskType.MergeRequest);
               oldTask.Cancel();
            }
            else if (oldTaskType == GitLabTaskType.Versions)
            {
               if (newTaskType != GitLabTaskType.Discussions
                && newTaskType != GitLabTaskType.CurrentUser)
               {
                  oldTask.Cancel();
               }
            }
            else if (oldTaskType == GitLabTaskType.Discussions
                  || oldTaskType == GitLabTaskType.CurrentUser)
            {
               if (newTaskType != GitLabTaskType.Discussions
                && newTaskType != GitLabTaskType.CurrentUser
                && newTaskType != GitLabTaskType.Versions)
               {
                  oldTask.Cancel();
               }
            }
            else
            {
               Debug.Assert(false);
            }
         }
      }

      private List<IGitLabTask> _RunningTasks = new List<IGitLabTask>();
   }
}

