using System;
using GitLabSharp.Accessors;
using GitLabSharp.Utils;
using mrHelper.GitLabClient.Operators;

namespace mrHelper.GitLabClient
{
   public class DataCacheException : ExceptionEx
   {
      internal DataCacheException(string message, Exception innerException)
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
                     if (wx.Response is System.Net.HttpWebResponse response
                      && response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
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

   public class DataCacheConnectionCancelledException : DataCacheException
   {
      internal DataCacheConnectionCancelledException()
         : base(String.Empty, null) {}
   }
}

