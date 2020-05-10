using System;
using System.Collections.Generic;

namespace mrHelper.Client.Common
{
   public class BaseNotifier<T> : INotifier<T>
   {
      public void AddListener(T listener)
      {
         _listeners.Add(listener);
      }

      public void RemoveListener(T listener)
      {
         _listeners.Remove(listener);
      }

      protected void notifyAll(Action<T> x)
      {
         _listeners.ForEach(y => x(y));
      }

      private List<T> _listeners = new List<T>();
   }
}

