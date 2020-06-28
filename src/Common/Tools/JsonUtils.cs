using System;
using System.Diagnostics;

namespace mrHelper.Common.Tools
{
   public static class JsonUtils
   {
      /// <summary>
      /// </summary>
      static public T LoadFromFile<T>(string filename)
      {
         Debug.Assert(System.IO.File.Exists(filename));
         string json = System.IO.File.ReadAllText(filename);
         return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
      }

      /// <summary>
      /// </summary>
      static public void SaveToFile(string filepath, object value)
      {
         string json = Newtonsoft.Json.JsonConvert.SerializeObject(value);
         System.IO.File.WriteAllText(filepath, json);
      }
   }
}

