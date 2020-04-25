using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel;
using mrHelper.Common.Exceptions;

namespace mrHelper.Common.Tools
{
   public static class TaskUtils
   {
      /// <summary>
      /// Runs a batch of functions of the given type simultaneously.
      /// Each function receives a single argument of type T which can be treated as a parallelized loop variable.
      /// </summary>
      async public static Task RunConcurrentFunctionsAsync<T>(IEnumerable<T> args, Func<T, Task> func,
         int concurrent, int interBatchDelay, Func<bool> shouldStop)
      {
         Debug.Assert(concurrent > 0);
         Debug.Assert(interBatchDelay >= 0);
         Debug.Assert(args != null);
         Debug.Assert(func != null);

         int remaining = args.Count();
         while (true)
         {
            Task[] tasks = args
               .Skip(args.Count() - remaining)
               .Take(concurrent)
               .Select(x => func(x))
               .ToArray();
            remaining -= tasks.Length;

            Task aggregateTask = Task.WhenAll(tasks);
            try
            {
               await aggregateTask;
            }
            catch (Exception)
            {
               if (aggregateTask.IsFaulted)
               {
                  foreach (Exception ex in aggregateTask.Exception.Flatten().InnerExceptions)
                  {
                     ExceptionHandlers.Handle("Batch task failed", ex);
                  }
               }
               else
               {
                  Debug.Assert(false);
               }
            }

            if ((shouldStop?.Invoke() ?? false) || remaining <= 0)
            {
               break;
            }

            if (interBatchDelay > 0)
            {
               await Task.Delay(interBatchDelay);
            }
         }
      }
   }
}

