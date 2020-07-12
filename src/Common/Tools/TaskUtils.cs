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
      async public static Task IfAsync(Func<bool> condition, int delay = 50)
      {
         if (condition())
         {
            await Task.Delay(delay);
         }
      }

      async public static Task WhileAsync(Func<bool> condition, int delay = 50)
      {
         while (condition()) //-V3120
         {
            await Task.Delay(delay);
         }
      }

      public struct BatchLimits
      {
         public int Size;
         public int Delay;
      }

      /// <summary>
      /// Runs a batch of functions of the given type simultaneously.
      /// Each function receives a single argument of type T which can be treated as a parallelized loop variable.
      /// </summary>
      async public static Task RunConcurrentFunctionsAsync<T>(IEnumerable<T> args, Func<T, Task> func,
         Func<BatchLimits> getBatchLimits, Func<bool> onBatchFinished)
      {
         Debug.Assert(getBatchLimits != null);
         Debug.Assert(onBatchFinished != null);
         Debug.Assert(args != null);
         Debug.Assert(func != null);

         int remaining = args.Count();
         while (true)
         {
            BatchLimits limits = getBatchLimits();
            if (limits.Size <= 0 || limits.Delay < 0)
            {
               throw new NotImplementedException();
            }

            Task[] tasks = args
               .Skip(args.Count() - remaining)
               .Take(limits.Size)
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

            if ((onBatchFinished?.Invoke() ?? false) || remaining <= 0)
            {
               break;
            }

            await IfAsync(() => limits.Delay > 0, limits.Delay);
         }
      }
   }
}

