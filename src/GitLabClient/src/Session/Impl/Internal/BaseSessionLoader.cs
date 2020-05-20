using System;
using System.Diagnostics;
using System.Threading.Tasks;
using GitLabSharp;
using GitLabSharp.Accessors;
using mrHelper.Client.Common;
using mrHelper.Common.Exceptions;

namespace mrHelper.Client.Session
{
   public class SessionException : ExceptionEx
   {
      internal SessionException(string message, Exception innerException)
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
   /// </summary>
   internal class BaseSessionLoader
   {
      internal BaseSessionLoader(SessionOperator op)
      {
         _operator = op;
      }

      async protected static Task<T> call<T>(Func<Task<T>> func, string cancelMessage, string errorMessage)
      {
         try
         {
            return await func();
         }
         catch (OperatorException ex)
         {
            if (ex.Cancelled)
            {
               Trace.TraceInformation(String.Format("[BaseSessionLoader] {0}", cancelMessage));
               return default(T);
            }
            throw new SessionException(errorMessage, ex);
         }
      }

      internal SessionOperator _operator;
   }
}

