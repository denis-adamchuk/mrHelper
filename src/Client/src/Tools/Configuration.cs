using System;
using System.Linq;
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
      private static readonly bool   CheckedLabelsFilterDefaultValue = false;

      private static readonly string LastUsedLabelsKeyName = "LastUsedLabels";
      private static readonly string LastUsedLabelsDefaultValue = "";

      private static readonly string ShowPublicOnlyKeyName = "ShowPublicOnly";
      private static readonly bool   ShowPublicOnlyDefaultValue = true;

      private static readonly string DiffContextDepthKeyName = "DiffContextDepth";
      private static readonly string DiffContextDepthDefaultValue = "2";

      private static readonly string MinimizeOnCloseKeyName = "MinimizeOnClose";
      private static readonly bool   MinimizeOnCloseDefaultValue = false;

      private static readonly string ColorSchemeFileNameKeyName = "ColorSchemeFileName";
      private static readonly string ColorSchemeFileNameDefaultValue = "";

      private static readonly string AutoUpdatePeriodMsKeyName      = "AutoUpdatePeriodMs";
      private static readonly int    AutoUpdatePeriodMsDefaultValue = 5 * 60 * 1000; // 5 minutes

      private static readonly string CacheRevisionsKeyName        = "CacheRevisionsInBackground";
      private static readonly bool   CacheRevisionsDefaultValue   = true;

      private static readonly string LogFilesToKeepKeyName = "LogFilesToKeep";
      private static readonly int    LogFilesToKeepDefaultValue = 10;

      private static readonly string Notifications_NewMergeRequests_KeyName      = "Notifications_NewMergeRequests";
      private static readonly bool   Notifications_NewMergeRequests_DefaultValue = true;

      private static readonly string Notifications_MergedMergeRequests_KeyName      = "Notifications_MergedMergeRequests";
      private static readonly bool   Notifications_MergedMergeRequests_DefaultValue = true;

      private static readonly string Notifications_UpdatedMergeRequests_KeyName      = "Notifications_UpdatedMergeRequests";
      private static readonly bool   Notifications_UpdatedMergeRequests_DefaultValue = true;

      private static readonly string Notifications_AllThreadsResolved_KeyName      = "Notifications_AllThreadsResolved";
      private static readonly bool   Notifications_AllThreadsResolved_DefaultValue = true;

      private static readonly string Notifications_OnMention_KeyName      = "Notifications_OnMention";
      private static readonly bool   Notifications_OnMention_DefaultValue = true;

      private static readonly string Notifications_Keywords_KeyName      = "Notifications_Keywords";
      private static readonly bool   Notifications_Keywords_DefaultValue = true;

      private static readonly string Notifications_MyActivity_KeyName      = "Notifications_MyActivity";
      private static readonly bool   Notifications_MyActivity_DefaultValue = false;

      private static readonly string Notifications_Service_KeyName      = "Notifications_Service";
      private static readonly bool   Notifications_Service_DefaultValue = false;

      private static readonly string ListViewMergeRequestsColumnWidthsKeyName      = "LVMR_ColWidths";
      private static readonly string ListViewMergeRequestsColumnWidthsDefaultValue = String.Empty;
      private static readonly int    ListViewMergeRequestsSingleColumnWidthDefaultValue = 100;

      private static readonly string MainWindowSplitterDistanceKeyName      = "MWSplitterDistance";
      private static readonly int    MainWindowSplitterDistanceDefaultValue = 0;

      private static readonly string RightPaneSplitterDistanceKeyName      = "RPSplitterDistance";
      private static readonly int    RightPaneSplitterDistanceDefaultValue = 0;

      private static readonly string VisualThemeNameKeyName       = "VisualThemeName";
      private static readonly string VisualThemeNameDefaultValue  = "New Year 2020";

      private static readonly string SelectedProjectsKeyName      = "SelectedProjects";
      private static readonly string SelectedProjectsDefaultValue = String.Empty;

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
         get
         {
            return bool.TryParse(getValue(
               CheckedLabelsFilterKeyName, boolToString(CheckedLabelsFilterDefaultValue)),
                  out bool result) ? result : CheckedLabelsFilterDefaultValue;
         }
         set { setValue(CheckedLabelsFilterKeyName, boolToString(value)); }
      }

      public string LastUsedLabels
      {
         get { return getValue(LastUsedLabelsKeyName, LastUsedLabelsDefaultValue); }
         set { setValue(LastUsedLabelsKeyName, value); }
      }

      public bool ShowPublicOnly
      {
         get
         {
            return bool.TryParse(getValue(
               ShowPublicOnlyKeyName, boolToString(ShowPublicOnlyDefaultValue)),
                  out bool result) ? result : ShowPublicOnlyDefaultValue;
         }
         set { setValue(ShowPublicOnlyKeyName, boolToString(value)); }
      }

      public bool MinimizeOnClose
      {
         get
         {
            return bool.TryParse(getValue(
               MinimizeOnCloseKeyName, boolToString(MinimizeOnCloseDefaultValue)),
                  out bool result) ? result : MinimizeOnCloseDefaultValue;
         }
         set { setValue(MinimizeOnCloseKeyName, boolToString(value)); }
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

      public string VisualThemeName
      {
         get { return getValue(VisualThemeNameKeyName, VisualThemeNameDefaultValue); }
         set { setValue(VisualThemeNameKeyName, value); }
      }

      public int LogFilesToKeep
      {
         get
         {
            return int.TryParse(getValue(
               LogFilesToKeepKeyName, LogFilesToKeepDefaultValue.ToString()),
                  out int result) ? LogFilesToKeepDefaultValue : result;
         }
         set { setValue(LogFilesToKeepKeyName, value.ToString()); }
      }

      public bool Notifications_NewMergeRequests
      {
         get
         {
            return bool.TryParse(getValue(
               Notifications_NewMergeRequests_KeyName, boolToString(Notifications_NewMergeRequests_DefaultValue)),
                  out bool result) ? result : Notifications_NewMergeRequests_DefaultValue;
         }
         set { setValue(Notifications_NewMergeRequests_KeyName, boolToString(value)); }
      }

      public bool Notifications_MergedMergeRequests
      {
         get
         {
            return bool.TryParse(getValue(
               Notifications_MergedMergeRequests_KeyName, boolToString(Notifications_MergedMergeRequests_DefaultValue)),
                  out bool result) ? result : Notifications_MergedMergeRequests_DefaultValue;
         }
         set { setValue(Notifications_MergedMergeRequests_KeyName, boolToString(value)); }
      }

      public bool Notifications_UpdatedMergeRequests
      {
         get
         {
            return bool.TryParse(getValue(
               Notifications_UpdatedMergeRequests_KeyName, boolToString(Notifications_UpdatedMergeRequests_DefaultValue)),
                  out bool result) ? result : Notifications_UpdatedMergeRequests_DefaultValue;
         }
         set { setValue(Notifications_UpdatedMergeRequests_KeyName, boolToString(value)); }
      }

      public bool Notifications_AllThreadsResolved
      {
         get
         {
            return bool.TryParse(getValue(
               Notifications_AllThreadsResolved_KeyName, boolToString(Notifications_AllThreadsResolved_DefaultValue)),
                  out bool result) ? result : Notifications_AllThreadsResolved_DefaultValue;
         }
         set { setValue(Notifications_AllThreadsResolved_KeyName, boolToString(value)); }
      }

      public bool Notifications_OnMention
      {
         get
         {
            return bool.TryParse(getValue(
               Notifications_OnMention_KeyName, boolToString(Notifications_OnMention_DefaultValue)),
                  out bool result) ? result : Notifications_OnMention_DefaultValue;
         }
         set { setValue(Notifications_OnMention_KeyName, boolToString(value)); }
      }

      public bool Notifications_Keywords
      {
         get
         {
            return bool.TryParse(getValue(
               Notifications_Keywords_KeyName, boolToString(Notifications_Keywords_DefaultValue)),
                  out bool result) ? result : Notifications_Keywords_DefaultValue;
         }
         set { setValue(Notifications_Keywords_KeyName, boolToString(value)); }
      }

      public bool Notifications_MyActivity
      {
         get
         {
            return bool.TryParse(getValue(
               Notifications_MyActivity_KeyName, boolToString(Notifications_MyActivity_DefaultValue)),
                  out bool result) ? result : Notifications_MyActivity_DefaultValue;
         }
         set { setValue(Notifications_MyActivity_KeyName, boolToString(value)); }
      }

      public bool Notifications_Service
      {
         get
         {
            return bool.TryParse(getValue(
               Notifications_Service_KeyName, boolToString(Notifications_Service_DefaultValue)),
                  out bool result) ? result : Notifications_Service_DefaultValue;
         }
         set { setValue(Notifications_Service_KeyName, boolToString(value)); }
      }

      public Dictionary<string, int> ListViewMergeRequestsColumnWidths
      {
         get
         {
            return stringToDictionary(getValue(
               ListViewMergeRequestsColumnWidthsKeyName, ListViewMergeRequestsColumnWidthsDefaultValue))
               .ToDictionary(
                  item => item.Key,
                  item => int.TryParse(item.Value, out int result) ?
                     result : ListViewMergeRequestsSingleColumnWidthDefaultValue);
         }
         set
         {
            setValue(ListViewMergeRequestsColumnWidthsKeyName,
               dictionaryToString(value.ToDictionary(item => item.Key, item => item.Value.ToString())));
         }
      }

      public int MainWindowSplitterDistance
      {
         get
         {
            return int.TryParse(getValue(
               MainWindowSplitterDistanceKeyName, MainWindowSplitterDistanceDefaultValue.ToString()),
                  out int result) ? result : MainWindowSplitterDistanceDefaultValue;
         }
         set { setValue(MainWindowSplitterDistanceKeyName, value.ToString()); }
      }

      public int RightPaneSplitterDistance
      {
         get
         {
            return int.TryParse(getValue(
               RightPaneSplitterDistanceKeyName, RightPaneSplitterDistanceDefaultValue.ToString()),
                  out int result) ? result : RightPaneSplitterDistanceDefaultValue;
         }
         set { setValue(RightPaneSplitterDistanceKeyName, value.ToString()); }
      }

      public int AutoUpdatePeriodMs
      {
         get
         {
            return int.TryParse(getValue(
               AutoUpdatePeriodMsKeyName, AutoUpdatePeriodMsDefaultValue.ToString()),
                  out int result) ? result : AutoUpdatePeriodMsDefaultValue;
         }
         set { setValue(AutoUpdatePeriodMsKeyName, value.ToString()); }
      }

      public bool CacheRevisionsInBackground
      {
         get
         {
            return bool.TryParse(getValue(
               CacheRevisionsKeyName, CacheRevisionsDefaultValue.ToString()),
                  out bool result) ? result : CacheRevisionsDefaultValue;
         }
         set { setValue(CacheRevisionsKeyName, value.ToString()); }
      }

      public bool HasSelectedProjects()
      {
         return _config.AppSettings.Settings[SelectedProjectsKeyName] != null;
      }

      public Dictionary<string, string> SelectedProjects
      {
         get
         {
            return stringToDictionary(getValue(SelectedProjectsKeyName, SelectedProjectsDefaultValue));
         }
         set
         {
            setValue(SelectedProjectsKeyName, dictionaryToString(value));
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

      private Dictionary<string, string> stringToDictionary(string value)
      {
         Dictionary<string, string> result = new Dictionary<string, string>();

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
            result.Add(subsplitted[0], subsplitted[1]);
         }

         return result;
      }

      private string dictionaryToString(Dictionary<string, string> value)
      {
         List<string> result = new List<string>();
         foreach (KeyValuePair<string, string> pair in value)
         {
            result.Add(pair.Key + "|" + pair.Value);
         }
         return String.Join(";", result);
      }

      private string boolToString(bool value)
      {
         return value.ToString().ToLower();
      }

      private void OnPropertyChanged(string name)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
      }

      private readonly Configuration _config;
      private readonly bool _changesAllowed;
   }
}

