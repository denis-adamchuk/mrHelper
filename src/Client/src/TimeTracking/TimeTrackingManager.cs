using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;
using mrHelper.Client.Workflow;

namespace mrHelper.Client.TimeTracking
{
   /// <summary>
   /// Manages time tracking for merge requests
   /// </summary>
   public class TimeTrackingManager
   {
      public TimeTrackingManager(UserDefinedSettings settings, Workflow.Workflow workflow)
      {
         Settings = settings;
         TimeTrackingOperator = new TimeTrackingOperator(Settings);
         workflow.PreLoadSystemNotes += () => PreLoadTotalTime?.Invoke();
         workflow.FailedLoadSystemNotes += () => FailedLoadTotalTime?.Invoke();
         workflow.PostLoadSystemNotes +=
            (state, notes) => processSystemNotes(state.MergeRequestDescriptor, state.CurrentUser, notes);
      }

      public event Action PreLoadTotalTime;
      public event Action FailedLoadTotalTime;
      public event Action<MergeRequestDescriptor> PostLoadTotalTime;

      public TimeSpan GetTotalTime(MergeRequestDescriptor mrd)
      {
         return MergeRequestTimes[mrd];
      }

      async public Task AddSpanAsync(bool add, TimeSpan span, MergeRequestDescriptor mrd)
      {
         await TimeTrackingOperator.AddSpanAsync(add, span, mrd);
         MergeRequestTimes[mrd] += span;
      }

      public TimeTracker GetTracker(MergeRequestDescriptor mrd)
      {
         return new TimeTracker(mrd, async (span, descriptor) => { await AddSpanAsync(true, span, descriptor); });
      }

      private static readonly Regex spentTimeRe =
         new Regex(
            @"^(?'operation'added|subtracted)\s(?>(?'hours'\d*)h\s)?(?>(?'minutes'\d*)m\s)?(?>(?'seconds'\d*)s\s)?of time spent.*",
               RegexOptions.Compiled);

      private void processSystemNotes(MergeRequestDescriptor mrd, User currentUser, List<Note> notes)
      {
         TimeSpan span = TimeSpan.Zero;
         foreach (Note note in notes)
         {
            if (note.Author.Id == currentUser.Id)
            {
               Match m = spentTimeRe.Match(note.Body);
               if (!m.Success)
               {
                  continue;
               }

               int hours = m.Groups["hours"].Success ? int.Parse(m.Groups["hours"].Value) : 0;
               int minutes = m.Groups["minutes"].Success ? int.Parse(m.Groups["minutes"].Value) : 0;
               int seconds = m.Groups["seconds"].Success ? int.Parse(m.Groups["seconds"].Value) : 0;
               if (m.Groups["operation"].Value == "added")
               {
                  span += new TimeSpan(hours, minutes, seconds);
               }
               else
               {
                  Debug.Assert(m.Groups["operation"].Value == "subtracted");
                  span -= new TimeSpan(hours, minutes, seconds);
               }
            }
         }

         MergeRequestTimes[mrd] = span;
         PostLoadTotalTime?.Invoke(mrd);
      }

      private UserDefinedSettings Settings { get; }
      private TimeTrackingOperator TimeTrackingOperator { get; }
      private Dictionary<MergeRequestDescriptor, TimeSpan> MergeRequestTimes { get; } =
         new Dictionary<MergeRequestDescriptor, TimeSpan>();
   }
}

