using System;
using System.Collections.Generic;
using System.Configuration;
using System.ComponentModel;

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

      private static readonly string LastSelectedProjectKeyName = "LastSelectedProject";
      private static readonly string LastSelectedProjectDefaultValue = "";

      private static readonly string LastSelectedHostKeyName = "LastSelectedHost";
      private static readonly string LastSelectedHostDefaultValue = "";

      private static readonly string ShowPublicOnlyKeyName = "ShowPublicOnly";
      private static readonly string ShowPublicOnlyDefaultValue = "true";

      private static readonly string DiffContextDepthKeyName = "DiffContextDepth";
      private static readonly string DiffContextDepthDefaultValue = "2";

      private static readonly string MinimizeOnCloseKeyName = "MinimizeOnClose";
      private static readonly string MinimizeOnCloseDefaultValue = "false";

      private static readonly string ColorSchemeFileNameKeyName = "ColorSchemeFileName";
      private static readonly string ColorSchemeFileNameDefaultValue = "";

      public event PropertyChangedEventHandler PropertyChanged;

      public UserDefinedSettings(bool changesAllowed)
      {
         _config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
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

      public string LastSelectedProject
      {
         get { return getValue(LastSelectedProjectKeyName, LastSelectedProjectDefaultValue); }
         set { setValue(LastSelectedProjectKeyName, value); }
      }

      public string LastSelectedHost
      {
         get { return getValue(LastSelectedHostKeyName, LastSelectedHostDefaultValue); }
         set { setValue(LastSelectedHostKeyName, value); }
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
            }
            return;
         }

         _config.AppSettings.Settings.Add(key, value);
         OnPropertyChanged(key);
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
