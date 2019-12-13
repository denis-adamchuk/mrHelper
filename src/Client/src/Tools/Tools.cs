using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Web.Script.Serialization;

namespace mrHelper.Client.Tools
{
   public static class Tools
   {
      /// <summary>
      /// Loads a list from file with JSON format
      /// Throws ArgumentException
      /// Throws ArgumentNullException
      /// Throws InvalidOperationException
      /// </summary>
      static public List<T> LoadListFromFile<T>(string filename)
      {
         Debug.Assert(System.IO.File.Exists(filename));

         string json = System.IO.File.ReadAllText(filename);

         JavaScriptSerializer serializer = new JavaScriptSerializer();

         List<T> items;
         try
         {
            items = serializer.Deserialize<List<T>>(json);
         }
         catch (Exception) // whatever de-serialization exception
         {
            throw;
         }

         return items;
      }

      static public Dictionary<string, object> LoadDictFromFile(string filename)
      {
         string json = System.IO.File.ReadAllText(filename);

         JavaScriptSerializer serializer = new JavaScriptSerializer();

         Dictionary<string, object> items;
         try
         {
            items = (Dictionary<string, object>)serializer.DeserializeObject(json);
         }
         catch (Exception) // whatever JSON exception
         {
            throw;
         }

         return items;
      }
   }
}

