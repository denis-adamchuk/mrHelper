using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace mrHelperUI
{
   internal class UserDefinedSettings
   {
      private static readonly string KnownHostsKeyName = "KnownHosts";
      private static readonly List<string> KnownHostsDefaultValue = new List<string>();

      private static readonly string KnownAccessTokensKeyName = "KnownAccessTokens";
      private static readonly List<string> KnownAccessTokensDefaultValue = new List<string>();

      private static readonly string LocalGitFolderKeyName = "LocalGitFolder";
      private static readonly string LocalGitFolderDefaultValue = Environment.GetEnvironmentVariable("TEMP");

      private static readonly string RequireTimeTrackingKeyName = "RequireTimeTracking";
      private static readonly string RequireTimeTrackingDefaultValue = "true";

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

      private static readonly string DiffContextAlgoKeyName = "DiffContextAlgo";
      private static readonly string DiffContextAlgoDefaultValue = "Combined";

      private static readonly string DiffContextDepthKeyName = "DiffContextDepth";
      private static readonly string DiffContextDepthDefaultValue = "3";

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

      public string RequireTimeTracking
      {
         get { return getValue(RequireTimeTrackingKeyName, RequireTimeTrackingDefaultValue); }
         set { setValue(RequireTimeTrackingKeyName, value); }
      }

      public string CheckedLabelsFilter
      {
         get { return getValue(CheckedLabelsFilterKeyName, CheckedLabelsFilterDefaultValue); }
         set { setValue(CheckedLabelsFilterKeyName, value); }
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

      public string ShowPublicOnly
      {
         get { return getValue(ShowPublicOnlyKeyName, ShowPublicOnlyDefaultValue); }
         set { setValue(ShowPublicOnlyKeyName, value); }
      }

      public string DiffContextAlgo
      {
         get { return getValue(DiffContextAlgoKeyName, DiffContextAlgoDefaultValue); }
         set { setValue(DiffContextAlgoKeyName, value); }
      }

      public string DiffContextDepth
      {
         get { return getValue(DiffContextDepthKeyName, DiffContextDepthDefaultValue); }
         set { setValue(DiffContextDepthKeyName, value); }
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

      private void setValues(string key, List<string> values)
      {
         string valuesString = string.Join(";", values);
         setValue(key, valuesString);
      }

      private readonly Configuration _config;
   }
}
