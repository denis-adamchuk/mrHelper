using System;
using System.Collections.Generic;
using System.Configuration;
using System.ComponentModel;
using System.Diagnostics;

namespace mrHelper.Client.Tools
{
   class ChangesNotAllowedException : Exception {}

   public class UserDefinedSettings : INotifyPropertyChanged
   {
      private static readonly string KnownHostsKeyName = "KnownHosts";
      private static readonly List<string> KnownHostsDefaultValue = new List<string>();

      private static readonly string KnownAccessTokensKeyName = "KnownAccessTokens";
      private static readonly List<string> KnownAccessTokensDefaultValue = new List<string>();

      private static readonly string LocalGitFolderKeyName = "LocalGitFolder";
      private static readonly string LocalGitFolderDefaultValue = Environment.GetEnvironmentVariable("TEMP");

      private static readonly string CheckedLabelsFilterKeyName = "CheckedLabelsFilter";
      private static readonly string CheckedLabelsFilterDefaultValue = "false";

      private static readonly string LastUsedLabelsKeyName = "LastUsedLabels";
      private static readonly string LastUsedLabelsDefaultValue = "";

      private static readonly string ShowPublicOnlyKeyName = "ShowPublicOnly";
      private static readonly string ShowPublicOnlyDefaultValue = "true";

      private static readonly string DiffContextDepthKeyName = "DiffContextDepth";
      private static readonly string DiffContextDepthDefaultValue = "2";

      private static readonly string MinimizeOnCloseKeyName = "MinimizeOnClose";
      private static readonly string MinimizeOnCloseDefaultValue = "false";

      private static readonly string ColorSchemeFileNameKeyName = "ColorSchemeFileName";
      private static readonly string ColorSchemeFileNameDefaultValue = "";

      private static readonly string LogFilesToKeepKeyName = "LogFilesToKeep";
      private static readonly int    LogFilesToKeepDefaultValue = 10;

      private static readonly string Notifications_NewMergeRequests_KeyName      = "Notifications_NewMergeRequests";
      private static readonly bool   Notifications_NewMergeRequests_DefaultValue = true;

      private static readonly string Notifications_MergedMergeRequests_KeyName      = "Notifications_MergedMergeRequests";
      private static readonly bool   Notifications_MergedMergeRequests_DefaultValue = true;

      private static readonly string Notifications_UpdatedMergeRequests_KeyName      = "Notifications_UpdatedMergeRequests";
      private static readonly bool   Notifications_UpdatedMergeRequests_DefaultValue = true;

      private static readonly string Notifications_ResolvedAllThreads_KeyName      = "Notifications_ResolvedAllThreads";
      private static readonly bool   Notifications_ResolvedAllThreads_DefaultValue = true;

      private static readonly string Notifications_OnMention_KeyName      = "Notifications_OnMention";
      private static readonly bool   Notifications_OnMention_DefaultValue = true;

      private static readonly string Notifications_Keywords_KeyName      = "Notifications_Keywords";
      private static readonly bool   Notifications_Keywords_DefaultValue = true;

      private static readonly string Notifications_MyActivity_KeyName      = "Notifications_MyActivity";
      private static readonly bool   Notifications_MyActivity_DefaultValue = false;

      public event PropertyChangedEventHandler PropertyChanged;

      public UserDefinedSettings(bool changesAllowed)
      {
         string configFilePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
               "mrHelper", "mrHelper.exe.config");

         ExeConfigurationFileMap configFileMap = new ExeConfigurationFileMap
         {
            ExeConfigFilename = configFilePath
         };

         _config = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);

         _changesAllowed = changesAllowed;
      }

      public void Update()
      {
         if (!_changesAllowed)
         {
            throw new ChangesNotAllowedException();
         }

         _config.Save(ConfigurationSaveMode.Full);
         ConfigurationManager.RefreshSection("appSettings");
      }

      // TODO Sync KnownHosts and KnownAccessTokens
      public List<string> KnownHosts
      {
         get { return getValues(KnownHostsKeyName, KnownHostsDefaultValue); }
         set { setValues(KnownHostsKeyName, value); }
      }

      public List<string> KnownAccessTokens
      {
         get { return getValues(KnownAccessTokensKeyName, KnownAccessTokensDefaultValue); }
         set { setValues(KnownAccessTokensKeyName, value); }
      }

      public string LocalGitFolder
      {
         get { return getValue(LocalGitFolderKeyName, LocalGitFolderDefaultValue); }
         set { setValue(LocalGitFolderKeyName, value); }
      }

      public bool CheckedLabelsFilter
      {
         get { return bool.Parse(getValue(CheckedLabelsFilterKeyName, CheckedLabelsFilterDefaultValue)); }
         set { setValue(CheckedLabelsFilterKeyName, value.ToString().ToLower()); }
      }

      public string LastUsedLabels
      {
         get { return getValue(LastUsedLabelsKeyName, LastUsedLabelsDefaultValue); }
         set { setValue(LastUsedLabelsKeyName, value); }
      }

      public bool ShowPublicOnly
      {
         get { return bool.Parse(getValue(ShowPublicOnlyKeyName, ShowPublicOnlyDefaultValue)); }
         set { setValue(ShowPublicOnlyKeyName, value.ToString().ToLower()); }
      }

      public bool MinimizeOnClose
      {
         get { return bool.Parse(getValue(MinimizeOnCloseKeyName, MinimizeOnCloseDefaultValue)); }
         set { setValue(MinimizeOnCloseKeyName, value.ToString().ToLower()); }
      }

      public string DiffContextDepth
      {
         get { return getValue(DiffContextDepthKeyName, DiffContextDepthDefaultValue); }
         set { setValue(DiffContextDepthKeyName, value); }
      }

      public string ColorSchemeFileName
      {
         get { return getValue(ColorSchemeFileNameKeyName, ColorSchemeFileNameDefaultValue); }
         set { setValue(ColorSchemeFileNameKeyName, value); }
      }

      public int LogFilesToKeep
      {
         get { return int.Parse(getValue(LogFilesToKeepKeyName, LogFilesToKeepDefaultValue.ToString())); }
         set { setValue(LogFilesToKeepKeyName, value.ToString()); }
      }

      public bool Notifications_NewMergeRequests
      {
         get { return bool.Parse(getValue(Notifications_NewMergeRequests_KeyName, Notifications_NewMergeRequests_DefaultValue.ToString())); }
         set { setValue(Notifications_NewMergeRequests_KeyName, value.ToString().ToLower()); }
      }

      public bool Notifications_MergedMergeRequests
      {
         get { return bool.Parse(getValue(Notifications_MergedMergeRequests_KeyName, Notifications_MergedMergeRequests_DefaultValue.ToString())); }
         set { setValue(Notifications_MergedMergeRequests_KeyName, value.ToString().ToLower()); }
      }

      public bool Notifications_UpdatedMergeRequests
      {
         get { return bool.Parse(getValue(Notifications_UpdatedMergeRequests_KeyName, Notifications_UpdatedMergeRequests_DefaultValue.ToString())); }
         set { setValue(Notifications_UpdatedMergeRequests_KeyName, value.ToString().ToLower()); }
      }

      public bool Notifications_ResolvedAllThreads
      {
         get { return bool.Parse(getValue(Notifications_ResolvedAllThreads_KeyName, Notifications_ResolvedAllThreads_DefaultValue.ToString())); }
         set { setValue(Notifications_ResolvedAllThreads_KeyName, value.ToString().ToLower()); }
      }

      public bool Notifications_OnMention
      {
         get { return bool.Parse(getValue(Notifications_OnMention_KeyName, Notifications_OnMention_DefaultValue.ToString())); }
         set { setValue(Notifications_OnMention_KeyName, value.ToString().ToLower()); }
      }

      public bool Notifications_Keywords
      {
         get { return bool.Parse(getValue(Notifications_Keywords_KeyName, Notifications_Keywords_DefaultValue.ToString())); }
         set { setValue(Notifications_Keywords_KeyName, value.ToString().ToLower()); }
      }

      public bool Notifications_MyActivity
      {
         get { return bool.Parse(getValue(Notifications_MyActivity_KeyName, Notifications_MyActivity_DefaultValue.ToString())); }
         set { setValue(Notifications_MyActivity_KeyName, value.ToString().ToLower()); }
      }

      public string GetAccessToken(string hostname)
      {
         for (int iKnownHost = 0; iKnownHost < KnownHosts.Count; ++iKnownHost)
         {
            if (hostname == KnownHosts[iKnownHost])
            {
               return KnownAccessTokens[iKnownHost];
            }
         }
         return String.Empty;
      }

      private string getValue(string key, string defaultValue)
      {
         if (_config.AppSettings.Settings[key] != null)
         {
            return _config.AppSettings.Settings[key].Value;
         }

         setValue(key, defaultValue);
         return defaultValue;
      }

      private void setValue(string key, string value)
      {
         if (!_changesAllowed)
         {
            throw new ChangesNotAllowedException();
         }

         if (_config.AppSettings.Settings[key] != null)
         {
            if (_config.AppSettings.Settings[key].Value != value)
            {
               _config.AppSettings.Settings[key].Value = value;
               OnPropertyChanged(key);

               Trace.TraceInformation(String.Format("[Configuration] Changed property {0} value to {1}", key, value));
            }
            return;
         }

         _config.AppSettings.Settings.Add(key, value);
         OnPropertyChanged(key);

         Trace.TraceInformation(String.Format("[Configuration] Added a new property {0} with value {1}", key, value));
      }

      private List<string> getValues(string key, List<string> defaultValues)
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

         setValues(key, defaultValues);
         return defaultValues;
      }

      private void setValues(string key, List<string> values)
      {
         string valuesString = string.Join(";", values);
         setValue(key, valuesString);
      }

      private void OnPropertyChanged(string name)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
      }

      private readonly Configuration _config;
      private readonly bool _changesAllowed;
   }
}
