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
   internal class TimeTrackingOperator : BaseOperator
   {
      internal TimeTrackingOperator(IHostProperties settings)
         : base(settings)
      {
      }

      internal Task AddSpanAsync(bool add, TimeSpan span, MergeRequestKey mrk)
      {
         return callWithNewClient(mrk.ProjectKey.HostName,
            async (client) =>
               await OperatorCallWrapper.CallNoCancel(
                  async () =>
                     await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.Get(mrk.ProjectKey.ProjectName).MergeRequests.Get(mrk.IId).AddSpentTimeAsync(
                              new AddSpentTimeParameters(add, span)))));
      }
   }
}

