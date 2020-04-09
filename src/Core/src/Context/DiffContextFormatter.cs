using System;
using System.Linq;

namespace mrHelper.Core.Context
{
   /// <summary>
   /// Renders DiffContext objects into HTML web page using CSS from resources
   /// </summary>
   public class DiffContextFormatter
   {
      public DiffContextFormatter(int fontSizePx, int rowsVPaddingPx)
      {
         _fontSizePx = fontSizePx;
         _rowsVPaddingPx = rowsVPaddingPx;
      }

      public string GetStylesheet()
      {
         return loadStylesFromCSS() + getCustomStyle(_fontSizePx, _rowsVPaddingPx);
      }

      /// <summary>
      /// Throws ArgumentException if DiffContext is invalid
      /// </summary>
      public string GetBody(DiffContext context)
      {
         return getContextHTML(context);
      }

      private string getContextHTML(DiffContext ctx)
      {
         string commonBegin = @"
               <body>
                  <table cellspacing=""0"" cellpadding=""0"">
                      <tbody>";

         string commonEnd = @"
                      </tbody>
                   </table>
                </body>";

         return commonBegin + getTableBody(ctx) + commonEnd;
      }

      private string loadStylesFromCSS()
      {
         return mrHelper.Core.Properties.Resources.DiffContextCSS;
      }

      private string getCustomStyle(int fontSizePx, int rowsVPaddingPx)
      {
         return string.Format(@"
            table {{
               font-size: {0}px;
            }}
            td {{
               padding-top: {1}px;
               padding-bottom: {1}px;
            }}", fontSizePx, rowsVPaddingPx);
      }

      private string getTableBody(DiffContext ctx)
      {
         bool highlightSelected = ctx.Lines.Count() > 1;
         string body = string.Empty;

         for (int iLine = 0; iLine < ctx.Lines.Count(); ++iLine)
         {
            DiffContext.Line line = ctx.Lines[iLine];

            body
              += "<tr" + (iLine == ctx.SelectedIndex && highlightSelected ? " class=\"selected\"" : "") + ">"
               + "<td class=\"linenumbers\">" + getLeftLineNumber(line) + "</td>"
               + "<td class=\"linenumbers\">" + getRightLineNumber(line) + "</td>"
               + "<td class=\"" + getDiffCellClass(line) + "\">" + getCode(line) + "</td>"
               + "</tr>";
         }
         return body;
      }

      private string getLeftLineNumber(DiffContext.Line line)
      {
         return line.Left?.Number.ToString() ?? "";
      }

      private string getRightLineNumber(DiffContext.Line line)
      {
         return line.Right?.Number.ToString() ?? "";
      }

      private string getDiffCellClass(DiffContext.Line line)
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

      private string getCode(DiffContext.Line line)
      {
         if (line.Text.Length == 0)
         {
            return "<br";
         }

         string trimmed = line.Text.TrimStart();
         int leadingSpaces = line.Text.Length - trimmed.Length;

         string spaces = string.Empty;
         for (int i = 0; i < leadingSpaces; ++i)
         {
            spaces += "&nbsp;";
         }

         return spaces + System.Net.WebUtility.HtmlEncode(trimmed);
      }

      private readonly int _fontSizePx;
      private readonly int _rowsVPaddingPx;
   }
}

