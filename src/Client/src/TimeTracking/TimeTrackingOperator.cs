using System;
using System.Diagnostics;
using System.Threading.Tasks;
using GitLabSharp;
using GitLabSharp.Accessors;
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
         GitLabClient client = new GitLabClient(mrd.HostName, Tools.Tools.GetAccessToken(mrd.HostName, Settings));
         try
         {
            await client.RunAsync(async (gitlab) =>
               await gitlab.Projects.Get(mrd.ProjectName).MergeRequests.Get(mrd.IId).AddSpentTimeAsync(
                  new AddSpentTimeParameters
                  {
                     Span = span
                  }));
         }
         catch (Exception ex)
         {
            Debug.Assert(!(ex is GitLabClientCancelled));
            if (ex is GitLabSharpException || ex is GitLabRequestException)
            {
               ExceptionHandlers.Handle(ex, "Cannot send tracked time to GitLab");
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      private UserDefinedSettings Settings { get; }
   }
}

