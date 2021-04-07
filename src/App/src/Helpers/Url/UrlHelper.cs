using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
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
         if (String.IsNullOrEmpty(originalUrl))
         {
            return null;
         }

         string url = trimPrefix(originalUrl);

         List<Exception> exceptions = new List<Exception>();
         try
         {
            UrlParser.ParsedMergeRequestUrl originalParsed = UrlParser.ParseMergeRequestUrl(url);
            UrlParser.ParsedMergeRequestUrl mergeRequestUrl = new UrlParser.ParsedMergeRequestUrl(
               StringUtils.GetHostWithPrefix(originalParsed.Host), originalParsed.Project, originalParsed.IId);
            return mergeRequestUrl;
         }
         catch (UriFormatException ex)
         {
            // ok, let's try another parser
            exceptions.Add(ex);
         }

         try
         {
            return NewMergeRequestUrlParser.Parse(url);
         }
         catch (UriFormatException ex)
         {
            // ok, input string cannot be parsed
            exceptions.Add(ex);
         }

         exceptions.ForEach(ex => ExceptionHandlers.Handle("Input URL cannot be parsed", ex));
         return null;
      }

      internal static bool CheckMergeRequestUrl(string originalUrl)
      {
         if (String.IsNullOrEmpty(originalUrl))
         {
            return false;
         }

         string url = trimPrefix(originalUrl);
         return UrlParser.IsValidUrl(url);
      }

      internal static void OpenBrowser(string url)
      {
         Trace.TraceInformation("Opening browser with URL {0}", url);

         try
         {
            Process.Start(url);
         }
         catch (Exception ex) // Any exception from Process.Start()
         {
            string errorMessage = "Cannot open URL";
            ExceptionHandlers.Handle(errorMessage, ex);
            MessageBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private static string trimPrefix(string originalUrl)
      {
         string prefix = Constants.CustomProtocolName + "://";
         return originalUrl.StartsWith(prefix) ? originalUrl.Substring(prefix.Length) : originalUrl;
      }
   }
}

