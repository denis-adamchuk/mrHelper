using Markdig;
using Markdig.Extensions;
using Markdig.Extensions.Tables;
using Markdig.Extensions.JiraLinks;
using mrHelper.Common.Interfaces;
using System;

namespace mrHelper.Common.Tools
{
   public static class MarkDownUtils
   {
      public static Markdig.MarkdownPipeline CreatePipeline(string jiraBaseUrl)
      {
         PipeTableOptions pipeTableOptions = new PipeTableOptions
         {
            RequireHeaderSeparator = false
         };

         MarkdownPipelineBuilder pipeline = new Markdig.MarkdownPipelineBuilder()
            .UseAutoLinks() // convert `http://...` into HTML `<a href ...>`
            .UsePipeTables(pipeTableOptions);

         if (!String.IsNullOrWhiteSpace(jiraBaseUrl))
         {
            pipeline.UseJiraLinks(new JiraLinkOptions(jiraBaseUrl));
         }

         return pipeline.Build();
      }

      public static string HtmlPageTemplate
      {
         get
         {
            return "<html><head></head><body><div> {0} </div></body></html>";
         }
      }

      public static string ConvertToHtml(string text, string uploadsPrefix, Markdig.MarkdownPipeline pipeline)
      {
         return System.Net.WebUtility
            .HtmlDecode(Markdig.Markdown.ToHtml(System.Net.WebUtility.HtmlEncode(text), pipeline))
            .Replace("<a href=\"/uploads/", String.Format("<a href=\"{0}/uploads/", uploadsPrefix))
            .Replace("<img src=\"/uploads/", String.Format("<img src=\"{0}/uploads/", uploadsPrefix));
      }
   }
}

