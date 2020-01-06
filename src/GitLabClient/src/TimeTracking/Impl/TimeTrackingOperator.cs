using System;
using System.Diagnostics;
using System.Threading.Tasks;
using GitLabSharp;
using GitLabSharp.Accessors;
using mrHelper.Client.Types;
using mrHelper.Client.Common;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.TimeTracking
{
   /// <summary>
   /// Implements Time-Tracking-related interaction with GitLab
   /// </summary>
   internal class TimeTrackingOperator
   {
      internal TimeTrackingOperator(IHostProperties settings)
      {
         _settings = settings;
      }

      async internal Task AddSpanAsync(bool add, TimeSpan span, MergeRequestKey mrk)
      {
         GitLabClient client = new GitLabClient(mrk.ProjectKey.HostName,
            _settings.GetAccessToken(mrk.ProjectKey.HostName));
         try
         {
            await client.RunAsync(async (gitlab) =>
               await gitlab.Projects.Get(mrk.ProjectKey.ProjectName).MergeRequests.Get(mrk.IId).AddSpentTimeAsync(
                  new AddSpentTimeParameters
                  {
                     Add = add,
                     Span = span
                  }));
         }
         catch (Exception ex)
         {
            Debug.Assert(!(ex is GitLabClientCancelled));
            if (ex is GitLabSharpException || ex is GitLabRequestException)
            {
               GitLabExceptionHandlers.Handle(ex, "Cannot send tracked time to GitLab");
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      private readonly IHostProperties _settings;
   }
}

