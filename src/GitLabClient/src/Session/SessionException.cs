using System;
using GitLabSharp.Accessors;
using GitLabSharp.Utils;
using mrHelper.Client.Common;

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

   public class SessionStartCancelledException : SessionException
   {
      internal SessionStartCancelledException()
         : base(String.Empty, null) {}
   }
}

