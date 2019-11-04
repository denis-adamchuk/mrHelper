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
         _settings = settings;
      }

      async internal Task AddSpanAsync(bool add, TimeSpan span, MergeRequestKey mrk)
      {
         GitLabClient client = new GitLabClient(mrk.ProjectKey.HostName,
            ConfigurationHelper.GetAccessToken(mrk.ProjectKey.HostName, _settings));
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
               ExceptionHandlers.Handle(ex, "Cannot send tracked time to GitLab");
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      private readonly UserDefinedSettings _settings;
   }
}

