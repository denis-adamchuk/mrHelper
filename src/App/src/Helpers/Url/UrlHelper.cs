﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using GitLabSharp;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Tools;

namespace mrHelper.App.Helpers
{
   internal static class UrlHelper
   {
      internal static object Parse(string originalUrl, Dictionary<string, string> sourceBranchTemplates)
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
            return NewMergeRequestUrlParser.Parse(url, sourceBranchTemplates);
         }
         catch (UriFormatException ex)
         {
            // ok, input string cannot be parsed
            exceptions.Add(ex);
         }

         exceptions.ForEach(ex => ExceptionHandlers.Handle("Input URL cannot be parsed", ex));
         return null;
      }

      private static readonly int MaxUrlLength = 256;

      internal static bool CheckGitLabMergeRequestUrl(string originalUrl)
      {
         if (String.IsNullOrEmpty(originalUrl) || originalUrl.Length > MaxUrlLength)
         {
            return false;
         }

         string url = trimPrefix(originalUrl);
         return UrlParser.IsValidMergeRequestUrl(url);
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

