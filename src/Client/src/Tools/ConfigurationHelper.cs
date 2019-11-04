using System;
using System.Linq;
using System.Collections.Generic;
using System.Configuration;
using System.ComponentModel;
using System.Diagnostics;

namespace mrHelper.Client.Tools
{
   public static class ConfigurationHelper
   {
      public static string GetAccessToken(string hostname, UserDefinedSettings settings)
      {
         for (int iKnownHost = 0; iKnownHost < settings.KnownHosts.Count; ++iKnownHost)
         {
            if (hostname == settings.KnownHosts[iKnownHost])
            {
               return settings.KnownAccessTokens[iKnownHost];
            }
         }
         return String.Empty;
      }

      public static string[] GetLabels(UserDefinedSettings settings)
      {
         if (!settings.CheckedLabelsFilter)
         {
             return null;
         }

         return settings.LastUsedLabels .Split(',').Select(x => x.Trim(' ')).ToArray();
      }
   }
}
