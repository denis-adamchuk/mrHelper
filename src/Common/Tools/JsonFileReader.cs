using System;
using System.Diagnostics;

namespace mrHelper.Common.Tools
{
   public static class JsonFileReader
   {
      /// <summary>
      /// Loads a list from file with JSON format
      /// </summary>
      static public T LoadFromFile<T>(string filename)
      {
         Debug.Assert(System.IO.File.Exists(filename));

         string json = System.IO.File.ReadAllText(filename);

         return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
      }
   }
}

