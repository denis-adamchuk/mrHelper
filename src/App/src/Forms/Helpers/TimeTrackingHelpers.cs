﻿using System;
using System.Diagnostics;
using GitLabSharp.Entities;
using mrHelper.Common.Constants;
using mrHelper.GitLabClient;

namespace mrHelper.App.Forms.Helpers
{
   internal static class TimeTrackingHelpers
   {
      public static string ConvertTotalTimeToText(TrackedTime trackedTime, bool isTimeTrackingAllowed)
      {
         if (trackedTime.Status == TrackedTime.EStatus.NotAvailable)
         {
            return "N/A";
         }

         if (trackedTime.Status == TrackedTime.EStatus.Loading)
         {
            return "Loading...";
         }

         Debug.Assert(trackedTime.Amount.HasValue);
         if (trackedTime.Amount.Value != TimeSpan.Zero)
         {
            return trackedTime.Amount.Value.ToString(@"hh\:mm\:ss");
         }

         return isTimeTrackingAllowed ? Constants.NotStartedTimeTrackingText : Constants.NotAllowedTimeTrackingText;
      }

      public static bool IsTimeTrackingAllowed(User mergeRequestAuthor, string hostname, User currentUser)
      {
         if (mergeRequestAuthor == null || String.IsNullOrWhiteSpace(hostname))
         {
            return true;
         }

         return  currentUser == null
              || currentUser.Id != mergeRequestAuthor.Id
              || Program.Settings.AllowAuthorToTrackTime;
      }
   }
}

