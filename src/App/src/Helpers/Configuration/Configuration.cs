using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Configuration;
using System.ComponentModel;
using System.Diagnostics;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;
using System.Drawing;

namespace mrHelper.App.Helpers
{
   class CorruptedSettingsException : Exception
   {
      internal CorruptedSettingsException(string configFilePath)
      {
         ConfigFilePath = configFilePath;
      }

      public string ConfigFilePath { get; }
   }

   public partial class UserDefinedSettings : IHostProperties, IFileStorageProperties
   {
      private static readonly string DefaultValuePrefix = ":";

      internal UserDefinedSettings()
      {
         string configFileName = Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName) + ".config";
         string configFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
               Constants.ApplicationDataFolderName, configFileName);

         ExeConfigurationFileMap configFileMap = new ExeConfigurationFileMap
         {
            ExeConfigFilename = configFilePath
         };

         try
         {
            _config = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);
         }
         catch (System.Configuration.ConfigurationErrorsException)
         {
            throw new CorruptedSettingsException(configFilePath);
         }
      }

      public string GetAccessToken(string host)
      {
         for (int iKnownHost = 0; iKnownHost < KnownHosts.Count(); ++iKnownHost)
         {
            if (host == KnownHosts[iKnownHost])
            {
               return KnownAccessTokens.Length > iKnownHost ? KnownAccessTokens[iKnownHost] : String.Empty;
            }
         }
         return String.Empty;
      }

      public int GetRevisionCountToKeep() => RevisionsToKeep;

      public int GetComparisonCountToKeep() => ComparisonsToKeep;

      public TaskUtils.BatchLimits GetComparisonBatchLimitsForAwaitedUpdate()
      {
         return new TaskUtils.BatchLimits
         {
            Size = AwaitedUpdateComparisonBatchSize,
            Delay = AwaitedUpdateComparisonBatchDelay
         };
      }

      public TaskUtils.BatchLimits GetFileBatchLimitsForAwaitedUpdate()
      {
         return new TaskUtils.BatchLimits
         {
            Size = AwaitedUpdateFileBatchSize,
            Delay = AwaitedUpdateFileBatchDelay
         };
      }

      public TaskUtils.BatchLimits GetComparisonBatchLimitsForNonAwaitedUpdate()
      {
         return new TaskUtils.BatchLimits
         {
            Size = NonAwaitedUpdateComparisonBatchSize,
            Delay = NonAwaitedUpdateComparisonBatchDelay
         };
      }

      public TaskUtils.BatchLimits GetFileBatchLimitsForNonAwaitedUpdate()
      {
         return new TaskUtils.BatchLimits
         {
            Size = NonAwaitedUpdateFileBatchSize,
            Delay = NonAwaitedUpdateFileBatchDelay
         };
      }

      private IEnumerable<string> getAccessTokens()
      {
         if (!AccessTokensProtected)
         {
            IEnumerable<string> values = getValues(KnownAccessTokensKeyName, Array.Empty<string>(), setValues);
            writeProtectedValues(KnownAccessTokensKeyName, values.ToArray());
            AccessTokensProtected = true;
         }
         return readProtectedValues(KnownAccessTokensKeyName, Array.Empty<string>());
      }

      private void setAccessTokens(IEnumerable<string> accessTokens)
      {
         writeProtectedValues(KnownAccessTokensKeyName, accessTokens.ToArray());
         AccessTokensProtected = true;
      }

      private Dictionary<string, int> getStringToIntDictionary(string keyName, string defaultValue,
         int fallbackValue, int errorValue)
      {
         return RawDictionaryStringHelper.DeserializeRawDictionaryString(getValue(keyName, defaultValue), false)
            .ToDictionary(
               item => item.Key,
               item => int.TryParse(item.Value, out int result) ? result : fallbackValue)
            .Where(x => x.Value != errorValue)
            .ToDictionary(
               item => item.Key,
               item => item.Value);
      }

      private void setStringToIntDictionary(string keyName, Dictionary<string, int> value)
      {
         setValue(keyName, RawDictionaryStringHelper.SerializeRawDictionaryString(
            value.ToDictionary(item => item.Key, item => item.Value.ToString())));
      }

      private Dictionary<string, string> getStringToStringDictionary(
         string keyName, string defaultValue, bool forceLowerCase)
      {
         return RawDictionaryStringHelper.DeserializeRawDictionaryString(
            getValue(keyName, defaultValue), forceLowerCase);
      }

      private void setStringToPointDictionary(string keyName, Dictionary<string, Point> value)
      {
         setValue(keyName, RawDictionaryStringHelper.SerializeRawDictionaryString(
            value.ToDictionary(item => item.Key, item => String.Format("{0},{1}", item.Value.X, item.Value.Y))));
      }

      private Dictionary<string, Point> getStringToPointDictionary(
         string keyName, string defaultValue)
      {
         return RawDictionaryStringHelper.DeserializeRawDictionaryString(getValue(keyName, defaultValue), false)
            .ToDictionary(
               item => item.Key,
               item =>
               {
                  string[] splitted = item.Value.Split(',');
                  if (splitted.Length == 2
                  && int.TryParse(splitted[0], out int x)
                  && int.TryParse(splitted[1], out int y))
                  {
                     return new Point(x, y);
                  }
                  return new Point(0, 0);
               })
            .ToDictionary(
               item => item.Key,
               item => item.Value);
      }

      private void setStringToStringDictionary(string keyName, Dictionary<string, string> value)
      {
         setValue(keyName, RawDictionaryStringHelper.SerializeRawDictionaryString(value));
      }

      private bool getBoolValue(string key, bool defaultValue)
      {
         return bool.TryParse(getValue(key, boolToString(defaultValue)), out bool result) ? result : defaultValue;
      }

      private void setBoolValue(string key, bool value)
      {
         setValue(key, boolToString(value));
      }

      private int getIntValue(string key, int defaultValue)
      {
         return int.TryParse(getValue(key, defaultValue.ToString()), out int result) ? result : defaultValue;
      }

      private void setIntValue(string key, int value)
      {
         setValue(key, value.ToString());
      }

      private string getValue(string key, string defaultValue)
      {
         KeyValueConfigurationElement currentValue = _config.AppSettings.Settings[key];
         if (currentValue != null && !currentValue.Value.StartsWith(DefaultValuePrefix))
         {
            return currentValue.Value;
         }

         setValue(key, DefaultValuePrefix + defaultValue);
         return defaultValue;
      }

      private void setValue(string key, string value)
      {
         bool notify = false;

         if (_config.AppSettings.Settings[key] == null)
         {
            _config.AppSettings.Settings.Add(key, value);
            notify = true;
            if (needLogValueChange(key))
            {
               Trace.TraceInformation("[Configuration] Added a new property {0} with value {1}", key, value);
            }
         }
         else if (_config.AppSettings.Settings[key].Value != value)
         {
            string oldValue = _config.AppSettings.Settings[key].Value;
            _config.AppSettings.Settings[key].Value = value;

            string oldValueWithoutPrefix = oldValue.StartsWith(DefaultValuePrefix)
               ? oldValue.Substring(DefaultValuePrefix.Length) : oldValue;
            string newValueWithoutPrefix = value.StartsWith(DefaultValuePrefix)
               ? value.Substring(DefaultValuePrefix.Length) : value;
            notify = oldValueWithoutPrefix != newValueWithoutPrefix;
            if (needLogValueChange(key))
            {
               Trace.TraceInformation("[Configuration] Changed value of property {0} from {1} to {2}, notify={3}",
                  key, oldValue, value, notify);
            }
         }

         if (notify)
         {
            onPropertyChanged(key);
         }
      }

      private bool needLogValueChange(string key)
      {
         return key != KnownAccessTokensKeyName;
      }

      private IEnumerable<string> getValues(string key, string[] defaultValues, Action<string, string[]> setter)
      {
         if (_config.AppSettings.Settings[key] != null)
         {
            var valuesString = _config.AppSettings.Settings[key].Value;
            List<string> values = new List<string>();
            if (valuesString.Length > 0)
            {
               foreach (var value in valuesString.Split(';'))
               {
                  values.Add(value);
               }
            }
            return values;
         }

         setter(key, defaultValues);
         return defaultValues;
      }

      private void setValues(string key, string[] values)
      {
         string valuesString = string.Join(";", values);
         setValue(key, valuesString);
      }

      /// <summary>
      /// Reads protected values by key, un-protects them and returns unprotected
      /// </summary>
      private IEnumerable<string> readProtectedValues(string key, string[] defaultRawValues)
      {
         List<string> rawValues = new List<string>();
         IEnumerable<string> protectedValues = getValues(key, defaultRawValues, writeProtectedValues);
         foreach (string protectedValue in protectedValues)
         {
            byte[] protectedBytes = Base64Helper.FromBase64StringSafe(protectedValue);
            byte[] rawBytes = CryptoHelper.UnprotectSafe(protectedBytes);
            string rawValue = StringUtils.GetStringSafe(rawBytes);
            rawValues.Add(rawValue ?? Constants.ConfigurationBadValueLoaded);
         }
         return rawValues;
      }

      /// <summary>
      /// Protects passed raw values and writes them to config by key
      /// </summary>
      private void writeProtectedValues(string key, string[] rawValues)
      {
         List<string> protectedStrings = new List<string>();
         foreach (string rawValue in rawValues)
         {
            byte[] rawBytes = StringUtils.GetBytesSafe(rawValue);
            byte[] protectedBytes = CryptoHelper.ProtectSafe(rawBytes);
            string protectedString = Base64Helper.ToBase64StringSafe(protectedBytes);
            protectedStrings.Add(protectedString ?? Constants.ConfigurationBadValueSaved);
         }
         setValues(key, protectedStrings.ToArray());
      }

      private string boolToString(bool value)
      {
         return value.ToString().ToLower();
      }

      private void onPropertyChanged(string keyName)
      {
         update();
         fireEventOnPropertyChange(keyName);
      }

      private void update()
      {
         try
         {
            _config.Save(ConfigurationSaveMode.Full);
         }
         catch (System.Configuration.ConfigurationErrorsException ex)
         {
            ExceptionHandlers.Handle("Cannot save configuration to disk", ex);
         }
         ConfigurationManager.RefreshSection("appSettings");
      }

      private void fireEventOnPropertyChange(string keyName)
      {
         if (keyName == WordWrapLongRowsKeyName)
         {
            WordWrapLongRowsChanged?.Invoke();
         }
         else if (keyName == MainWindowLayoutKeyName)
         {
            MainWindowLayoutChanged?.Invoke();
         }
         else if (keyName == DiffContextPositionKeyName)
         {
            DiffContextPositionChanged?.Invoke();
         }
         else if (keyName == DiscussionColumnWidthKeyName)
         {
            DiscussionColumnWidthChanged?.Invoke();
         }
         else if (keyName == NeedShiftRepliesKeyName)
         {
            NeedShiftRepliesChanged?.Invoke();
         }
      }

      private readonly Configuration _config;
   }
}

