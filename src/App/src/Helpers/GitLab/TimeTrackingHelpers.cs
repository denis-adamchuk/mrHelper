using System;
using System.Diagnostics;
using GitLabSharp.Entities;
using mrHelper.Common.Constants;
using mrHelper.GitLabClient;

namespace mrHelper.App.Helpers
{
   internal static class TimeTrackingHelpers
   {
      public static string ConvertTotalTimeToText(TrackedTime trackedTime, bool isTimeTrackingAllowed, bool compact)
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
            return Common.Tools.TimeUtils.TimeSpanToString(trackedTime.Amount.Value, compact);
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

