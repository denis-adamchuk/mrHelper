using System;
using System.Diagnostics;

namespace mrHelper.Common.Tools
{
   public static class TimeUtils
   {
      public static string TimeSpanToStringAgo(TimeSpan timeSpan)
      {
         string timeSpanAsText = TimeSpanToString(timeSpan);
         return timeSpanAsText == ZeroTimeSpanText ? timeSpanAsText : timeSpanAsText + " ago";
      }

      public static string TimeSpanToString(TimeSpan timeSpan)
      {
         if (timeSpan.Seconds >= 55)
         {
            timeSpan = timeSpan.Add(new TimeSpan(0, 1, 0));
         }

         timeSpan = new TimeSpan(timeSpan.Days, timeSpan.Hours, timeSpan.Minutes, 0);
         Debug.Assert(timeSpan.Seconds == 0);
         Debug.Assert(timeSpan.Milliseconds == 0);

         string formatNumber(double number, string name)
         {
            int rounded = Convert.ToInt32(Math.Floor(number));
            return String.Format("{1} {0}{2}", name, rounded, rounded > 1 ? "s" : "");
         }

         double totalMonths = Math.Floor(timeSpan.TotalDays / 30);
         double totalWeeks = Math.Floor(timeSpan.TotalDays / 7);
         double totalDays = Math.Floor(timeSpan.TotalDays);
         double totalHours = Math.Floor(timeSpan.TotalHours);
         double totalMinutes = Math.Floor(timeSpan.TotalMinutes);
         if (totalMonths > 0)
         {
            return formatNumber(totalMonths, "month");
         }
         else if (totalWeeks > 0)
         {
            return formatNumber(totalWeeks, "week");
         }
         else if (totalDays > 0)
         {
            return formatNumber(totalDays, "day");
         }
         else if (totalHours > 0)
         {
            return formatNumber(totalHours, "hour");
         }
         else if (totalMinutes > 0)
         {
            return formatNumber(totalMinutes, "minute");
         }
         else
         {
            return ZeroTimeSpanText;
         }
      }

      private static readonly string ZeroTimeSpanText = "just now";
   }
}

