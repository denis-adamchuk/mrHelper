using System;

namespace mrHelper.Common.Tools
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

