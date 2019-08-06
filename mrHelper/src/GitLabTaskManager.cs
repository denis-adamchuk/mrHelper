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
   /// 
   /// </summary>
   public enum GitLabTaskType
   {
      Projects,
      MergeRequests,
      MergeRequest,
      Versions,
      Discussions,
      CurrentUser
   }

   /// <summary>
   /// 
   /// </summary>
   public class GitLabTaskManager
   {
      /// <summary>
      /// 
      /// </summary>
      public interface IGitLabTask
      {
         bool IsCancelled();
         void Cancel();
         GitLabTaskType GetTaskType();
         void Wait();
      }

      /// <summary>
      /// 
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
         /// 
         /// </summary>
         public TaskAwaiter<TResult> GetAwaiter()
         {
            return Task.GetAwaiter();
         }

         /// <summary>
         /// 
         /// </summary>
         public bool IsCancelled()
         {
            return Cancelled;
         }

         /// <summary>
         /// 
         /// </summary>
         public void Cancel()
         {
            Cancelled = true;
         }

         /// <summary>
         /// 
         /// </summary>
         public GitLabTaskType GetTaskType()
         {
            return Type;
         }

         /// <summary>
         /// 
         /// </summary>
         public void Wait()
         {
            Task.Wait();
         }

         private bool Cancelled { get; set; }
         private Task<TResult> Task { get; }
         private GitLabTaskType Type { get; }
      }

      public GitLabTask<TResult> CreateTask<TResult>(Task<TResult> task, GitLabTaskType type)
      {
         var gitLabTask = new GitLabTask<TResult>(task, type);
         cancelByType(type);
         _RunningTasks.Add(gitLabTask);
         return gitLabTask;
      }

      /// <summary>
      /// 
      /// </summary>
      async public Task<TResult> RunAsync<TResult>(GitLabTask<TResult> gitLabTask) where TResult : new()
      {
         TResult taskResult = await gitLabTask;
         _RunningTasks.Remove(gitLabTask);
         return gitLabTask.IsCancelled() ? default(TResult) : taskResult;
      }

      /// <summary>
      /// 
      /// </summary>
      public void CancelAll(GitLabTaskType type)
      {
         cancelByType(type);
      }

      /// <summary>
      /// 
      /// </summary>
      public void WaitAll()
      {
         foreach (var task in _RunningTasks)
         {
            task.Wait();
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

