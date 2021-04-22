using System;
using System.Collections.Generic;
using System.Diagnostics;

using RawDictionaryString   = System.Collections.Generic.Dictionary<string, string>;

namespace mrHelper.App.Helpers
{
   /// <summary>
   /// Supports working with configuration strings in format {Key}|{Value};{Key}|{Value};...
   /// </summary>
   internal static class RawDictionaryStringHelper
   {
      internal static RawDictionaryString DeserializeRawDictionaryString(string value, bool forceKeyLowerCase)
      {
         RawDictionaryString result = new RawDictionaryString();

         string[] splitted = value.Split(';');
         foreach (string splittedItem in splitted)
         {
            if (!splittedItem.Contains("|"))
            {
               Debug.Assert(splittedItem == String.Empty);
               continue;
            }

            string[] subsplitted = splittedItem.Split('|');
            if (subsplitted.Length != 2)
            {
               Debug.Assert(false);
               continue;
            }
            result.Add(forceKeyLowerCase ? subsplitted[0].ToLower() : subsplitted[0], subsplitted[1]);
         }

         return result;
      }

      internal static string SerializeRawDictionaryString(RawDictionaryString value)
      {
         List<string> result = new List<string>();
         foreach (KeyValuePair<string, string> pair in value)
         {
            result.Add(pair.Key + "|" + pair.Value);
         }
         return String.Join(";", result);
      }

   }
}

