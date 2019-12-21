using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Web.Script.Serialization;

namespace mrHelper.CommonTools
{
   public static class JsonFileReader
   {
      /// <summary>
      /// Loads a list from file with JSON format
      /// Throws ArgumentException
      /// Throws ArgumentNullException
      /// Throws InvalidOperationException
      /// </summary>
      static public T LoadFromFile<T>(string filename)
      {
         Debug.Assert(System.IO.File.Exists(filename));

         string json = System.IO.File.ReadAllText(filename);

         JavaScriptSerializer serializer = new JavaScriptSerializer();

         T items;
         try
         {
            items = serializer.Deserialize<T>(json);
         }
         catch (Exception) // whatever de-serialization exception
         {
            throw;
         }

         return items;
      }
   }
}

