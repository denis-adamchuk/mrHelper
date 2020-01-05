using System;

namespace mrHelper.CommonTools.Persistence
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

