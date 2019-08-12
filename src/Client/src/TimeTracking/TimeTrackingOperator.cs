using System;
using GitLabSharp;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;

namespace mrHelper.Client.TimeTracking
{
   /// <summary>
   /// Implements Time-Tracking-related interaction with GitLab
   /// </summary>
   internal class TimeTrackingOperator
   {
      internal TimeTrackingOperator(UserDefinedSettings settings)
      {
         Settings = settings;
      }

      internal Task<TimeSpan> GetSpanAsync(MergeRequestDescriptor mrd)
      {
         throw new NotImplementedException();
      }

      async internal Task AddSpanAsync(TimeSpan span, MergeRequestDescriptor mrd)
      {
         GitLabClient client = new GitLabClient(mrd.HostName, Tools.GetAccessToken(mrd.HostName, Settings);
         try
         {
            return await client.RunAsync(async (gitlab) =>
               return await gitlab.Projects.Get(mrd.ProjectName).MergeRequests.Get(mrd.IId).AddSpentTimeAsync(
                  new AddSpentTimeParameters
                  {
                     Span = span
                  }));
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot send tracked time to GitLab");
            throw new OperatorException(ex);
         }
      }

      private Settings Settings { get; }
   }
}

