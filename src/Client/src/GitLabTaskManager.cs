using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace mrHelper.Client
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

      private List<IGitLabTask> _RunningTasks = new List<IGitLabTask>();
   }
}

