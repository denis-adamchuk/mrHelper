using System;
using System.Collections.Generic;
using GitLabSharp;
using mrHelper.App.Helpers.GitLab;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Tools;

namespace mrHelper.App.Helpers
{
   internal static class UrlHelper
   {
      internal static object Parse(string originalUrl)
      {
         string prefix = Constants.CustomProtocolName + "://";
         string url = originalUrl.StartsWith(prefix) ? originalUrl.Substring(prefix.Length) : originalUrl;

         List<Exception> exceptions = new List<Exception>();
         try
         {
            UrlParser.ParsedMergeRequestUrl originalParsed = UrlParser.ParseMergeRequestUrl(url);
            UrlParser.ParsedMergeRequestUrl mergeRequestUrl = new UrlParser.ParsedMergeRequestUrl(
               StringUtils.GetHostWithPrefix(originalParsed.Host), originalParsed.Project, originalParsed.IId);
            return mergeRequestUrl;
         }
         catch (Exception ex)
         {
            // ok, let's try another parser
            exceptions.Add(ex);
         }

         try
         {
            return NewMergeRequestUrlParser.Parse(url);
         }
         catch (Exception ex)
         {
            // ok, input string cannot be parsed
            exceptions.Add(ex);
         }

         exceptions.ForEach(ex => ExceptionHandlers.Handle("Input URL cannot be parsed", ex));
         return null;
      }
   }
}

