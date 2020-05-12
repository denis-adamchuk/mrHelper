using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp;
using GitLabSharp.Accessors;
using mrHelper.Client.Common;
using mrHelper.Common.Exceptions;

namespace mrHelper.Client.Session
{
   public class WorkflowException : ExceptionEx
   {
      internal WorkflowException(string message, Exception innerException)
         : base(message, innerException) {}

      public string UserMessage
      {
         get
         {
            if (InnerException is OperatorException ox)
            {
               if (ox.InnerException is GitLabRequestException rx)
               {
                  if (rx.InnerException is System.Net.WebException wx)
                  {
                     System.Net.HttpWebResponse response = wx.Response as System.Net.HttpWebResponse;
                     if (response != null && response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                     {
                        return wx.Message + " Check your access token!";
                     }
                     return wx.Message;
                  }
               }
            }
            return OriginalMessage;
         }
      }
   }

   /// <summary>
   /// Supports chains of actions (loading a merge request also loads its versions or commits)
   /// Each action toggles Pre-{Action}-Event and either Post-{Action}-Event or Failed-{Action}-Event
   /// </summary>
   internal class BaseSessionLoader
   {
      internal BaseSessionLoader(SessionOperator op)
      {
         _operator = op;
      }

      async public Task CancelAsync()
      {
         if (_operator != null)
         {
            await _operator.CancelAsync();
         }
      }

      internal void handleOperatorException(OperatorException ex, string cancelMessage, string errorMessage,
         IEnumerable<Action> failureActions)
      {
         bool cancelled = ex.InnerException is GitLabClientCancelled;
         if (cancelled)
         {
            Trace.TraceInformation(String.Format("[BaseWorkflowLoader] {0}", cancelMessage));
            return;
         }

         failureActions?.ToList().ForEach(x => x?.Invoke());

         throw new WorkflowException(errorMessage, ex);
      }

      internal SessionOperator _operator;
   }
}

