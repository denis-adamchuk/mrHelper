﻿using System;
using System.Linq;
using System.Text;
using mrHelper.Common.Tools;

namespace mrHelper.Core.Context
{
   /// <summary>
   /// Renders DiffContext objects into HTML web page using CSS from resources
   /// </summary>
   public static class DiffContextFormatter
   {
      /// <summary>
      /// Throws ArgumentException if DiffContext is invalid
      /// </summary>
      public static string GetHtml(DiffContext context, double fontSizePt, int? tableWidth,
         ContextColorProvider provider)
      {
         if (!context.IsValid())
         {
            string errorMessage = "Cannot render HTML context.";
            return String.Format("<html><body>{0} See logs for details</body></html>", errorMessage);
         }

         return getHtml(getTable(context), fontSizePt, tableWidth, provider);
      }

      public static string GetHtml(string code, double fontSizePt, int? tableWidth,
         ContextColorProvider provider)
      {
         return getHtml(getTable(code), fontSizePt, tableWidth, provider);
      }

      private static string getHtml(string table, double fontSizePt, int? tableWidth,
         ContextColorProvider provider)
      {
         return String.Format(
            @"<html>
               <head>
                  <style>
                     {0}
                  </style>
               </head>
               <body>
                  {1}
               </body>
             </html>",
            getStylesheet(fontSizePt, tableWidth, provider), table);
      }

      private static string getStylesheet(double fontSizePt, int? tableWidth,
         ContextColorProvider provider)
      {
         return loadStylesFromCSS() + getCustomStyle(fontSizePt, tableWidth) + applyColors(provider);
      }

      static readonly string TableBegin = @"<table cellspacing=""0"" cellpadding=""0"">";
      static readonly string TableEnd = @"</table>";

      private static string getTable(string text)
      {
         return String.Format("{0} {1} {2}", TableBegin, getTableBody(text), TableEnd);
      }

      private static string getTable(DiffContext ctx)
      {
         return String.Format("{0} {1} {2}", TableBegin, getTableBody(ctx), TableEnd);
      }

      private static string loadStylesFromCSS()
      {
         return mrHelper.Core.Properties.Resources.DiffContextCSS;
      }

      private static string getCustomStyle(double fontSizePt, int? tableWidth)
      {
         return string.Format(@"
            table {{
               font-size: {0}pt;
               width: {1};
            }}
            td {{
               overflow: visible;
            }}",
            fontSizePt, tableWidth.HasValue ? (tableWidth.Value.ToString() + "px") : "100%");
      }

      private static string applyColors(ContextColorProvider provider)
      {
         return string.Format(@"
            td.linenumbers {{
               color: {0};
               background-color: {1};
               border-right: solid 1px {2};
            }}
            td.unchanged {{
               color: {3};
               background-color: {4};
            }}
            td.removed {{
               color: {5};
               background-color: {6};
            }}
            td.added {{
               color: {7};
               background-color: {8};
            }}",
            HtmlUtils.ColorToRgb(provider.LineNumbersColor),
            HtmlUtils.ColorToRgb(provider.LineNumbersBackgroundColor),
            HtmlUtils.ColorToRgb(provider.LineNumbersRightBorderColor),
            HtmlUtils.ColorToRgb(provider.UnchangedColor),
            HtmlUtils.ColorToRgb(provider.UnchangedBackgroundColor),
            HtmlUtils.ColorToRgb(provider.RemovedColor),
            HtmlUtils.ColorToRgb(provider.RemovedBackgroundColor),
            HtmlUtils.ColorToRgb(provider.AddedColor),
            HtmlUtils.ColorToRgb(provider.AddedBackgroundColor));
      }


      private static string getTableBody(string text)
      {
         StringBuilder body = new StringBuilder();
         body.Append("<tr class=\"selected\">"); // emulate bold just in case
         body.Append("<td class=\"linenumbers\">999</td>");
         body.Append("<td class=\"linenumbers\">999</td>");
         body.AppendFormat("<td class=\"unchanged\">{0}</td>", textToHtml(text));
         body.Append("</tr>");
         return body.ToString();
      }

      private static string getTableBody(DiffContext ctx)
      {
         bool highlightSelected = ctx.Lines.Count() > 1;
         StringBuilder body = new StringBuilder();

         int iLine = 0;
         foreach (DiffContext.Line line in ctx.Lines)
         {
            body.Append("<tr");
            body.Append((iLine == ctx.SelectedIndex && highlightSelected ? " class=\"selected\"" : ""));
            body.Append(">");
            body.Append("<td class=\"linenumbers\">");
            body.Append(getLeftLineNumber(line));
            body.Append("</td>");
            body.Append("<td class=\"linenumbers\">");
            body.Append(getRightLineNumber(line));
            body.Append("</td>");
            body.Append("<td class=\"");
            body.Append(getDiffCellClass(line));
            body.Append("\">");
            body.Append(getCode(line));
            body.Append("</td>");
            body.Append("</tr>");

            ++iLine;
         }
         return body.ToString();
      }

      private static string getLeftLineNumber(DiffContext.Line line)
      {
         return line.Left?.Number.ToString() ?? "";
      }

      private static string getRightLineNumber(DiffContext.Line line)
      {
         return line.Right?.Number.ToString() ?? "";
      }

      private static string getDiffCellClass(DiffContext.Line line)
      {
         if (line.Left.HasValue && line.Right.HasValue)
         {
            return "unchanged";
         }
         else if (line.Left.HasValue)
         {
            return line.Left.Value.State == DiffContext.Line.State.Unchanged ? "unchanged" : "removed";
         }
         else if (line.Right.HasValue)
         {
            return line.Right.Value.State == DiffContext.Line.State.Unchanged ? "unchanged" : "added";
         }

         throw new ArgumentException(String.Format("Bad context line: {0}", line.ToString()));
      }

      private static string getCode(DiffContext.Line line)
      {
         return textToHtml(line.Text);
      }

      private static string textToHtml(string text)
      {
         if (text.Length == 0)
         {
            return "<br>";
         }

         // replace some special symbols such as '<' or '>'
         string encodedText = System.Net.WebUtility.HtmlEncode(text);

         // replace spaces with &nbsp
         return encodedText
            .Replace("\t", "    ")   /* replace each TAB with four spaces */
            .Replace(" ", "&nbsp;"); /* replace each SPACE with &nbsp; */
      }
   }
}

