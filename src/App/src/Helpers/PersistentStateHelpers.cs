using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using mrHelper.App.Forms.Helpers;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;
using mrHelper.GitLabClient;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace mrHelper.App.Helpers
{
   internal static class PersistentStateHelper
   {
      internal static readonly string DateTimeFormat = "yyyyMMdd-HHmmss";
   }

   internal class PersistentStateLoadHelper
   {
      internal PersistentStateLoadHelper(string recordName, IPersistentStateGetter reader)
      {
         _recordName = recordName;
         _reader = reader;
      }

      internal void Load(out string value)
      {
         value = _reader.Get(_recordName) as string;
      }

      internal void Load(out HashSet<ProjectKey> values)
      {
         values = readObjectAsArray()?
            .Select(token => loadProjectKey(token))
            .ToHashSet();
      }

      internal void Load(out HashSet<MergeRequestKey> values)
      {
         values = readObjectAsArray()?
            .Select(token => loadMergeRequestKey(token))
            .ToHashSet();
      }

      internal void Load(out Dictionary<MergeRequestKey, HashSet<string>> values)
      {
         values = readObjectAsDict(_reader, _recordName)?
            .ToDictionary(
               item => loadMergeRequestKey(item.Key),
               item => item.Value is JArray jArray ? jarrayToStringCollection(jArray).ToHashSet() : new HashSet<string>());
      }

      internal void Load(out Dictionary<MergeRequestKey, DateTime> values)
      {
         values = readObjectAsDict(_reader, _recordName)?
            .ToDictionary(
               item => loadMergeRequestKey(item.Key),
               item => loadDateTime(item.Value as string));
      }

      internal void Load(out Dictionary<string, MergeRequestKey> values)
      {
         values = readObjectAsDict(_reader, _recordName)?
            .ToDictionary(
               item => loadProjectKey(item.Key).HostName,
               item => new MergeRequestKey(loadProjectKey(item.Key), loadIId(item.Value as string)));
      }

      internal void Load(out Dictionary<string, NewMergeRequestProperties> values)
      {
         values = readObjectAsDict(_reader, _recordName)?
            .Where(x => ((x.Value as string) ?? String.Empty).Split('|').Length == 4)
            .ToDictionary(
               item => item.Key,
               item =>
               {
                  string[] splitted = (item.Value as string).Split('|');
                  return new NewMergeRequestProperties(
                     splitted[0],
                     null,
                     null,
                     splitted[1],
                     splitted[2] == bool.TrueString,
                     splitted[3] == bool.TrueString);
               });
      }

      private static int loadIId(string keyAsText)
      {
         return int.TryParse(keyAsText, out int parsedIId) ? parsedIId : 0;
      }

      private static DateTime loadDateTime(string dtAsText)
      {
         DateTime defaultDateTimeForRecentMergeRequests = DateTime.Now;
         return DateTime.TryParseExact(
            dtAsText, PersistentStateHelper.DateTimeFormat, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out DateTime dt) ? dt : defaultDateTimeForRecentMergeRequests;
      }

      private static MergeRequestKey loadMergeRequestKey(string keyAsText)
      {
         string[] splitted = keyAsText.Split('|');
         if (splitted.Length != 3)
         {
            Debug.Assert(false);
            return default(MergeRequestKey);
         }

         string projectKeyAsText = String.Join("|", splitted[0], splitted[1]);
         return new MergeRequestKey(loadProjectKey(projectKeyAsText), loadIId(splitted[2]));
      }

      private static ProjectKey loadProjectKey(string keyAsText)
      {
         string[] splitted = keyAsText.Split('|');
         if (splitted.Length != 2)
         {
            Debug.Assert(false);
            return default(ProjectKey);
         }

         string host = splitted[0];
         string projectName = splitted[1];
         return new ProjectKey(host, projectName);
      }

      private string[] readObjectAsArray()
      {
         return _reader.Get(_recordName) is JArray jArray ? jarrayToStringCollection(jArray).ToArray() : null;
      }

      private Dictionary<string, object> readObjectAsDict(IPersistentStateGetter reader, string objectName)
      {
         JObject newMergeRequestDialogStatesByHostsObj = reader.Get(objectName) as JObject;
         return newMergeRequestDialogStatesByHostsObj?.ToObject<Dictionary<string, object>>();
      }

      private IEnumerable<string> jarrayToStringCollection(JArray jArray)
      {
         Debug.Assert(jArray != null);
         return jArray
               .Cast<JToken>()
               .Where(token => token.Type == JTokenType.String)
               .Select(token => token.Value<string>());
      }

      private readonly string _recordName;
      private readonly IPersistentStateGetter _reader;
   }

   internal class PersistentStateSaveHelper
   {
      internal PersistentStateSaveHelper(string recordName, IPersistentStateSetter writer)
      {
         _recordName = recordName;
         _writer = writer;
      }

      internal void Save(string hostname)
      {
         _writer.Set(_recordName, hostname);
      }

      internal void Save(HashSet<ProjectKey> values)
      {
         IEnumerable<string> valuesSerialized = values.Select(item => saveProjectKey(item));
         _writer.Set(_recordName, valuesSerialized);
      }

      internal void Save(HashSet<MergeRequestKey> values)
      {
         IEnumerable<string> valuesSerialized = values.Select(item => saveMergeRequestKey(item));
         _writer.Set(_recordName, valuesSerialized);
      }

      internal void Save(Dictionary<MergeRequestKey, HashSet<string>> values)
      {
         Dictionary<string, HashSet<string>> valuesSerialized = values
            .ToDictionary(
               item => saveMergeRequestKey(item.Key),
               item => item.Value);
         _writer.Set(_recordName, valuesSerialized);
      }

      internal void Save(Dictionary<MergeRequestKey, DateTime> values)
      {
         Dictionary<string, string> valuesSerialized = values
            .ToDictionary(
               item => saveMergeRequestKey(item.Key),
               item => saveDateTime(item.Value));
         _writer.Set(_recordName, valuesSerialized);
      }

      internal void Save(Dictionary<string, MergeRequestKey> values)
      {
         Dictionary<string, string> valuesSerialized = values
            .ToDictionary(
               item => saveProjectKey(item.Value.ProjectKey),
               item => saveIId(item.Value.IId));
         _writer.Set(_recordName, valuesSerialized);
      }

      internal void Save(Dictionary<string, NewMergeRequestProperties> values)
      {
         Dictionary<string, string> valuesSerialized = values
            .ToDictionary(
               item => item.Key,
               item => item.Value.DefaultProject
               + "|" + item.Value.AssigneeUsername
               + "|" + item.Value.IsBranchDeletionNeeded.ToString()
               + "|" + item.Value.IsSquashNeeded.ToString());
         _writer.Set(_recordName, valuesSerialized);
      }

      private static string saveIId(int iid)
      {
         return iid.ToString();
      }

      private static string saveDateTime(DateTime dt)
      {
         return dt.ToString(PersistentStateHelper.DateTimeFormat);
      }

      private static string saveMergeRequestKey(MergeRequestKey key)
      {
         return saveProjectKey(key.ProjectKey) + "|" + saveIId(key.IId);
      }

      private static string saveProjectKey(ProjectKey key)
      {
         return key.HostName + "|" + key.ProjectName;
      }

      private readonly string _recordName;
      private readonly IPersistentStateSetter _writer;
   }
}

