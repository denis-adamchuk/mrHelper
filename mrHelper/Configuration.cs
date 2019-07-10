using System;
using System.Collections.Generic;
using System.Configuration;
using System.ComponentModel;

namespace mrHelperUI
{
   class UserDefinedSettings : INotifyPropertyChanged
   {
      private static string KnownHostsKeyName = "KnownHosts";
      private static List<string> KnownHostsDefaultValue = new List<string>();

      private static string KnownAccessTokensKeyName = "KnownAccessTokens";
      private static List<string> KnownAccessTokensDefaultValue = new List<string>();

      private static string LocalGitFolderKeyName = "LocalGitFolder";
      private static string LocalGitFolderDefaultValue = Environment.GetEnvironmentVariable("TEMP");

      private static string RequireTimeTrackingKeyName = "RequireTimeTracking";
      private static string RequireTimeTrackingDefaultValue = "true";

      private static string CheckedLabelsFilterKeyName = "CheckedLabelsFilter";
      private static string CheckedLabelsFilterDefaultValue = "false";

      private static string LastUsedLabelsKeyName = "LastUsedLabels";
      private static string LastUsedLabelsDefaultValue = "";

      private static string LastSelectedProjectKeyName = "LastSelectedProject";
      private static string LastSelectedProjectDefaultValue = "";

      private static string LastSelectedHostKeyName = "LastSelectedHost";
      private static string LastSelectedHostDefaultValue = "";

      private static string ShowPublicOnlyKeyName = "ShowPublicOnly";
      private static string ShowPublicOnlyDefaultValue = "true";

      private static string MinimizeOnCloseKeyName = "MinimizeOnClose";
      private static string MinimizeOnCloseDefaultValue = "false";

      public event PropertyChangedEventHandler PropertyChanged;

      public UserDefinedSettings()
      {
         _config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
      }

      public void Update()
      {
         _config.Save(ConfigurationSaveMode.Full);
         ConfigurationManager.RefreshSection("appSettings");
      }

      // TODO Sync KnownHosts and KnownAccessTokens
      public List<string> KnownHosts
      {
         get { return getValues(KnownHostsKeyName, KnownHostsDefaultValue); }
         set
         {
            setValues(KnownHostsKeyName, value);
            OnPropertyChanged(KnownHostsKeyName);
         }
      }

      public List<string> KnownAccessTokens
      {
         get { return getValues(KnownAccessTokensKeyName, KnownAccessTokensDefaultValue); }
         set
         {
            setValues(KnownAccessTokensKeyName, value);
            OnPropertyChanged(KnownAccessTokensKeyName);
         }
      }

      public string LocalGitFolder
      {
         get { return getValue(LocalGitFolderKeyName, LocalGitFolderDefaultValue); }
         set
         {
            setValue(LocalGitFolderKeyName, value);
            OnPropertyChanged(LocalGitFolderKeyName);
         }
      }

      public bool RequireTimeTracking
      {
         get { return bool.Parse(getValue(RequireTimeTrackingKeyName, RequireTimeTrackingDefaultValue)); }
         set
         {
            setValue(RequireTimeTrackingKeyName, value.ToString().ToLower());
            OnPropertyChanged(RequireTimeTrackingKeyName);
         }
      }

      public bool CheckedLabelsFilter
      {
         get { return bool.Parse(getValue(CheckedLabelsFilterKeyName, CheckedLabelsFilterDefaultValue)); }
         set
         {
            setValue(CheckedLabelsFilterKeyName, value.ToString().ToLower());
            OnPropertyChanged(CheckedLabelsFilterKeyName);
         }
      }

      public string LastUsedLabels
      {
         get { return getValue(LastUsedLabelsKeyName, LastUsedLabelsDefaultValue); }
         set
         {
            setValue(LastUsedLabelsKeyName, value);
            OnPropertyChanged(LastUsedLabelsKeyName);
         }
      }

      public string LastSelectedProject
      {
         get { return getValue(LastSelectedProjectKeyName, LastSelectedProjectDefaultValue); }
         set
         {
            setValue(LastSelectedProjectKeyName, value);
            OnPropertyChanged(LastSelectedProjectKeyName);
         }
      }

      public string LastSelectedHost
      {
         get { return getValue(LastSelectedHostKeyName, LastSelectedHostDefaultValue); }
         set
         {
            setValue(LastSelectedHostKeyName, value);
            OnPropertyChanged(LastSelectedHostKeyName);
         }
      }

      public bool ShowPublicOnly
      {
         get { return bool.Parse(getValue(ShowPublicOnlyKeyName, ShowPublicOnlyDefaultValue)); }
         set
         {
            setValue(ShowPublicOnlyKeyName, value.ToString().ToLower());
            OnPropertyChanged(ShowPublicOnlyKeyName);
         }
      }

      public bool MinimizeOnClose
      {
         get { return bool.Parse(getValue(MinimizeOnCloseKeyName, MinimizeOnCloseDefaultValue)); }
         set
         {
            setValue(MinimizeOnCloseKeyName, value.ToString().ToLower());
            OnPropertyChanged(MinimizeOnCloseKeyName);
         }
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
         if (_config.AppSettings.Settings[key] != null)
         {
            _config.AppSettings.Settings[key].Value = value;
            return;
         }

         _config.AppSettings.Settings.Add(key, value);
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

      private void OnPropertyChanged(string name)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
      }

      private void setValues(string key, List<string> values)
      {
         string valuesString = string.Join(";", values);
         setValue(key, valuesString);
      }

      private Configuration _config;
   }
}
