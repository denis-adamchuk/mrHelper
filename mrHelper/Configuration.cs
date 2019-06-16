using System;
using System.Configuration;
using System.Linq;

namespace mrHelper
{
   class UserDefinedSettings
   {
      private static string HostKeyName = "Host";
      private static string HostDefaultValue = "";

      private static string AccessTokenKeyName = "AccessToken";
      private static string AccessTokenDefaultValue = "";

      private static string LocalGitFolderKeyName = "LocalGitFolder";
      private static string LocalGitFolderDefaultValue = Environment.GetEnvironmentVariable("TEMP");

      public UserDefinedSettings()
      {
         _config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
      }
      
      public void Update()
      {
         _config.Save(ConfigurationSaveMode.Full);
         ConfigurationManager.RefreshSection("appSettings");
      }

      public string Host
      {
         get { return getValue(HostKeyName, HostDefaultValue); }
         set { setValue(HostKeyName, value); }
      }

      public string AccessToken
      {
         get { return getValue(AccessTokenKeyName, AccessTokenDefaultValue); }
         set { setValue(AccessTokenKeyName, value); }
      }

      public string LocalGitFolder
      {
         get { return getValue(LocalGitFolderKeyName, LocalGitFolderDefaultValue); }
         set { setValue(LocalGitFolderKeyName, value); }
      }

      private string getValue(string key, string defaultValue)
      {
         if (_config.AppSettings.Settings[key] != null)
         {
            return _config.AppSettings.Settings[key].Value;
         }

         _config.AppSettings.Settings.Add(key, defaultValue);
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

      private Configuration _config;
   }
}
