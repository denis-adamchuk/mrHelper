using System;

namespace mrHelper.Client.Persistence
{
   public interface IPersistentStateGetter
   {
      object Get(string key);
   }

   public interface IPersistentStateSetter
   {
      void Set(string key, object obj);
   }
}

