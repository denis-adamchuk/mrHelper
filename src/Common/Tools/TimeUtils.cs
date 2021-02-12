using System;
using System.Diagnostics;

namespace mrHelper.Common.Tools
{
   public static class TimeUtils
   {
      public static string TimeStampFormat = "d-MMM-yyyy HH:mm";

      public static string DateTimeOptToString(DateTime? dateTime)
      {
         return dateTime.HasValue ? DateTimeToString(dateTime.Value) : "N/A";
      }

      public static string DateTimeToString(DateTime dateTime)
      {
         return dateTime.ToLocalTime().ToString(TimeStampFormat);
      }

      public static string DateTimeOptToStringAgo(DateTime? dateTime)
      {
         return dateTime.HasValue ? DateTimeToStringAgo(dateTime.Value) : "N/A";
      }

      public static string DateTimeToStringAgo(DateTime dateTime)
      {
         return TimeSpanToStringAgo(DateTime.Now - dateTime.ToLocalTime());
      }

      public static string TimeSpanToStringAgo(TimeSpan timeSpan)
      {
         string timeSpanAsText = TimeSpanToString(timeSpan);
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

      public static string TimeSpanToString(TimeSpan timeSpan)
      {
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
         double totalDays = Math.Floor(timeSpan.TotalDays);
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
         else if (totalDays > 0)
         {
            return formatNumber(totalDays, DayText);
         }
         else if (totalHours > 0)
         {
            return formatNumber(totalHours, HourText);
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

      private static readonly string YesterdayText = "yesterday";
      private static readonly string ZeroTimeSpanText = "just now";
      private static readonly string DayTimeSpanText  = "a day";
      private static readonly string MonthText = "month";
      private static readonly string WeekText = "week";
      private static readonly string DayText = "day";
      private static readonly string HourText = "hour";
      private static readonly string MinuteText = "minute";

      /*
            TimeSpanToString():
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(0, 0, 0))          == "just now");
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(0, 0, 1))          == "just now");
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(0, 0, 35))         == "just now");
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(0, 0, 55))         == "a minute");
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(0, 0, 59))         == "a minute");
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(0, 1, 0))          == "a minute");
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(0, 1, 55))         == "2 minutes");
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(0, 2, 05))         == "2 minutes");
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(0, 2, 05))         == "2 minutes");
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(0, 20, 59))        == "21 minutes");
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(0, 25, 05))        == "25 minutes");
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(1, 0, 0))          == "an hour");
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(1, 1, 1))          == "an hour");
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(1, 30, 0))         == "an hour");
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(1, 49, 0))         == "an hour");
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(1, 49, 49))        == "an hour");
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(1, 49, 50))        == "2 hours");
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(1, 50, 0))         == "2 hours");
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(1, 59, 59))        == "2 hours");
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(2, 45, 0))         == "2 hours");
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(23, 0, 0))         == "23 hours");
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(23, 45, 0))        == "23 hours");
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(23, 49, 50))       == "a day");
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(23, 50, 0))        == "a day");
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(1, 0, 0, 0))       == "a day");
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(1, 16, 59, 59))    == "a day");
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(1, 23, 49, 50))    == "2 days");
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(1, 23, 59, 50))    == "2 days");
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(6, 23, 59, 50))    == "7 days");
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(13, 23, 59, 50))   == "14 days");
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(19, 23, 59, 50))   == "20 days");
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(20, 23, 59, 50))   == "3 weeks");
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(22, 23, 59, 50))   == "3 weeks");
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(26, 23, 59, 50))   == "3 weeks");
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(27, 0, 0, 0))      == "3 weeks");
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(27, 23, 49, 49))   == "3 weeks");
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(27, 23, 49, 50))   == "4 weeks");
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(29, 0, 0, 0))      == "4 weeks");
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(30, 0, 0, 0))      == "a month");
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(59, 0, 0, 0))      == "a month");
         Debug.Assert(TimeUtils.TimeSpanToString(new TimeSpan(59, 29, 50, 50))   == "2 months");
      */
   }
}

