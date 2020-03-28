using mrHelper.Common.Interfaces;
using System;

namespace mrHelper.Common.Tools
{
   public static class MarkDownUtils
   {
      public static Markdig.MarkdownPipeline CreatePipeline()
      {
         Markdig.Extensions.Tables.PipeTableOptions options = new Markdig.Extensions.Tables.PipeTableOptions
         {
            RequireHeaderSeparator = false
         };
         return Markdig.MarkdownExtensions
            .UsePipeTables(new Markdig.MarkdownPipelineBuilder(), options)
            .Build();
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
            .Replace("<img src=\"/uploads/", String.Format("<img src=\"{0}/uploads/", uploadsPrefix));
      }
   }
}

