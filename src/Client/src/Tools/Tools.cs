using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Configuration;
using System.ComponentModel;
using System.Web.Script.Serialization;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;
using mrHelper.Client.Updates;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;
using mrHelper.CustomActions;

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
   }
}

