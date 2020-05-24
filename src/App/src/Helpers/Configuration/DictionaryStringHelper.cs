using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

using RawDictionaryString   = System.Collections.Generic.Dictionary<string, string>; // Key = {Host}, Value = the rest
using DictionaryStringKey   = System.String; // {Host}
using DictionaryStringValue = System.Collections.Generic.IEnumerable<System.Tuple<string, string>>; // the rest

namespace mrHelper.App.Helpers
{
   /// <summary>
   /// Supports working with configuration strings in format {Host}|{Property}:{Value},{Property:Value},...
   /// </summary>
   public static class DictionaryStringHelper
   {
      public static RawDictionaryString DeserializeRawDictionaryString(string value, bool forceKeyLowerCase)
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

      public static string SerializeRawDictionaryString(RawDictionaryString value)
      {
         List<string> result = new List<string>();
         foreach (KeyValuePair<string, string> pair in value)
         {
            result.Add(pair.Key + "|" + pair.Value);
         }
         return String.Join(";", result);
      }

      public static void UpdateRawDictionaryString(
         Dictionary<DictionaryStringKey, DictionaryStringValue> newValues,
         RawDictionaryString dictionaryString)
      {
         Dictionary<DictionaryStringKey, DictionaryStringValue> parsedRecord =
            parseRawDictionaryString(dictionaryString);

         foreach (KeyValuePair<string, DictionaryStringValue> kv in newValues)
         {
            parsedRecord[kv.Key] = kv.Value;
         }

         dictionaryString.Clear();
         foreach (KeyValuePair<string, DictionaryStringValue> kv in parsedRecord)
         {
            dictionaryString.Add(kv.Key,
               String.Join(",", kv.Value.Select(x => x.Item1.ToString() + ":" + x.Item2.ToString())));
         }
      }

      public static DictionaryStringValue GetDictionaryStringValue(
         DictionaryStringKey key, RawDictionaryString dictionaryString)
      {
         if (String.IsNullOrEmpty(key) || !dictionaryString.ContainsKey(key))
         {
            return Array.Empty<Tuple<string, string>>();
         }
         return deserializeDictionaryStringValue(dictionaryString[key]);
      }

      private static Dictionary<DictionaryStringKey, DictionaryStringValue> parseRawDictionaryString(
         RawDictionaryString dictionaryString)
      {
         return dictionaryString
            .ToDictionary(
               item => item.Key,
               item => deserializeDictionaryStringValue(item.Value));
      }

      private static DictionaryStringValue deserializeDictionaryStringValue(string items)
      {
         if (String.IsNullOrEmpty(items))
         {
            return Array.Empty<Tuple<string, string>>();
         }

         return items
            .Split(',')
            .Where(x => x.Split(':').Length == 2)
            .Select(x =>
            {
               string[] splitted = x.Split(':');
               return new Tuple<string, string>(splitted[0], splitted[1]);
            });
      }
   }
}

