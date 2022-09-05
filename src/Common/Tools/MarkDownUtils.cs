using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Extensions.JiraLinks;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;

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
            return "<html><head></head><body class=\"no-border no-bg\"><div class=\"no-border no-bg\"> {0} </div></body></html>";
         }
      }

      public static string ConvertToHtml(string text, string uploadsPrefix,
         Markdig.MarkdownPipeline pipeline, Control control)
      {
         if (String.IsNullOrEmpty(text))
         {
            return String.Empty;
         }

         // fix #319, most likely this is just a workaround for Markdig bug
         text = text.Replace("<details>", "").Replace("</details>", "");

         string html = System.Net.WebUtility
            .HtmlDecode(Markdig.Markdown.ToHtml(System.Net.WebUtility.HtmlEncode(text), pipeline))
            .Replace("<a href=\"/uploads/", String.Format("<a href=\"{0}/uploads/", uploadsPrefix))
            .Replace("<img src=\"/uploads/", String.Format("<img src=\"{0}/uploads/", uploadsPrefix));
         html = HtmlUtils.AddWidthAttributeToCodeElements(html, new WidthCalculator(control).CalculateWidth);
         return HtmlUtils.WrapImageIntoTables(html);
      }
   }
}

