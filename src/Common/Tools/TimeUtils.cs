using System;
using System.Diagnostics;

namespace mrHelper.Common.Tools
{
   public static class TimeUtils
   {
      public const string DefaultTimeStampFormat = "d-MMM-yyyy HH:mm";
      public const string DateOnlyTimeStampFormat = "d-MMM-yyyy";
      public const string DateOnlyGitLabFormat = "yyyy-MM-dd";

      public static string DateTimeOptToString(DateTime? dateTime)
      {
         return dateTime.HasValue ? DateTimeToString(dateTime.Value) : "N/A";
      }

      public static string DateTimeToString(DateTime dateTime, string format = DefaultTimeStampFormat)
      {
         return dateTime.ToLocalTime().ToString(format);
      }

      public static string DateTimeOptToStringAgo(DateTime? dateTime)
      {
         return dateTime.HasValue ? DateTimeToStringAgo(dateTime.Value) : "N/A";
      }

      public static string DateTimeToStringAgo(DateTime dateTime)
      {
         DateTime laterTime = DateTime.Now;
         DateTime earlyTime = dateTime.ToLocalTime();
         string timeSpanAsText = TimeSpanToString(laterTime, earlyTime);
         if (timeSpanAsText == DayTimeSpanText)
         {
            return YesterdayText;
         }
         else if (timeSpanAsText == ZeroTimeSpanText)
         {
            return ZeroTimeSpanText;
         }
         return String.Format("{0} {1}", timeSpanAsText, "ago");
      }

      public static string TimeSpanToString(DateTime laterTime, DateTime earlyTime)
      {
         var timeSpan = laterTime - earlyTime;
         if (timeSpan.Seconds >= 50)
         {
            timeSpan = timeSpan.Add(new TimeSpan(0, 1, 0));
            timeSpan = new TimeSpan(timeSpan.Days, timeSpan.Hours, timeSpan.Minutes, 0);
         }
         if (timeSpan.Minutes >= 50)
         {
            timeSpan = timeSpan.Add(new TimeSpan(1, 0, 0));
            timeSpan = new TimeSpan(timeSpan.Days, timeSpan.Hours, 0, 0);
         }

         timeSpan = new TimeSpan(timeSpan.Days, timeSpan.Hours, timeSpan.Minutes, 0);
         Debug.Assert(timeSpan.Seconds == 0);
         Debug.Assert(timeSpan.Milliseconds == 0);

         string formatNumber(double number, string name)
         {
            int intNumber = Convert.ToInt32(Math.Floor(number));
            if (intNumber == 1 && name == DayText)
            {
               return DayTimeSpanText;
            }
            return String.Format("{0} {1}{2}",
               intNumber > 1 ? intNumber.ToString() : (name == HourText ? "an" : "a"),
               name,
               intNumber > 1 ? "s" : "");
         }

         double totalMonths = Math.Floor(timeSpan.TotalDays / 30);
         double totalWeeks = Math.Floor(timeSpan.TotalDays / 7);
         double totalHours = Math.Floor(timeSpan.TotalHours);
         double totalMinutes = Math.Floor(timeSpan.TotalMinutes);
         if (totalMonths > 0)
         {
            return formatNumber(totalMonths, MonthText);
         }
         else if (totalWeeks > 2)
         {
            return formatNumber(totalWeeks, WeekText);
         }
         else if (totalHours > 0)
         {
            // It is bad from UX perspective to treat spans within a range [24h;48h) as 1 day back
            // Let's check real distance in days.
            DateTime laterTimeRounded = new DateTime(laterTime.Year, laterTime.Month, laterTime.Day);
            DateTime earlyTimeRounded = new DateTime(earlyTime.Year, earlyTime.Month, earlyTime.Day);
            double totalDays = Math.Floor((laterTimeRounded - earlyTimeRounded).TotalDays);
            double number = totalDays > 0 ? totalDays : totalHours;
            string text = totalDays > 0 ? DayText : HourText;
            return formatNumber(number, text);
         }
         else if (totalMinutes > 0)
         {
            return formatNumber(totalMinutes, MinuteText);
         }
         else
         {
            return ZeroTimeSpanText;
         }
      }

      public static TimeSpan GetTimeTillMorning()
      {
         return getTimeTillMorning(1);
      }

      public static TimeSpan GetTimeTillMonday()
      {
         int daysTillMonday = getDaysTillMonday();
         return getTimeTillMorning(daysTillMonday);
      }

      private static TimeSpan getTimeTillMorning(int dayOffset)
      {
         int morningHour = 08;
         DateTime nextDay = DateTime.Now.AddDays(dayOffset);
         DateTime nextDayMorning = new DateTime(nextDay.Year, nextDay.Month, nextDay.Day, morningHour, 0, 0);
         return nextDayMorning - DateTime.Now;
      }

      private static int getDaysTillMonday()
      {
         switch (DateTime.Now.DayOfWeek)
         {
            case DayOfWeek.Monday:    return 7;
            case DayOfWeek.Tuesday:   return 6;
            case DayOfWeek.Wednesday: return 5;
            case DayOfWeek.Thursday:  return 4;
            case DayOfWeek.Friday:    return 3;
            case DayOfWeek.Saturday:  return 2;
            case DayOfWeek.Sunday:    return 1;
         }
         Debug.Assert(false);
         return 0;
      }

      private static readonly string YesterdayText = "yesterday";
      private static readonly string ZeroTimeSpanText = "just now";
      private static readonly string DayTimeSpanText  = "a day";
      private static readonly string MonthText = "month";
      private static readonly string WeekText = "week";
      private static readonly string DayText = "day";
      private static readonly string HourText = "hour";
      private static readonly string MinuteText = "minute";
   }
}

