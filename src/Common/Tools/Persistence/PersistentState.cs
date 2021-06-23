using System.Collections.Generic;
using System.Diagnostics;

namespace mrHelper.Common.Tools
{
   public class PersistentState : IPersistentStateGetter, IPersistentStateSetter
   {
      public object Get(string key)
      {
         Debug.Assert(_state != null);
         return _state.ContainsKey(key) ? _state[key] : null;
      }

      public void Set(string key, object obj)
      {
         Debug.Assert(_state != null);
         _state[key] = obj;
      }

      internal PersistentState()
      {
         _state = new Dictionary<string, object>();
      }

      internal PersistentState(string json)
      {
         var state = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
         _state = state ?? new Dictionary<string, object>();
      }

      internal string ToJson()
      {
         return Newtonsoft.Json.JsonConvert.SerializeObject(_state);
      }

      private readonly Dictionary<string, object> _state = new Dictionary<string, object>();
   }
}

