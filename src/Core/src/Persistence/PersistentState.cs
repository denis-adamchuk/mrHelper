using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Web.Script.Serialization;

namespace mrHelper.Client.Persistence
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
         JavaScriptSerializer serializer = new JavaScriptSerializer();
         _state = serializer.Deserialize<Dictionary<string, object>>(json);
      }

      internal string ToJson()
      {
         JavaScriptSerializer serializer = new JavaScriptSerializer();
         return serializer.Serialize(_state);
      }

      private readonly Dictionary<string, object> _state = new Dictionary<string, object>();
   }
}

