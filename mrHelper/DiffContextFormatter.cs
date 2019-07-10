using mrCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace mrHelperUI
{
   public class DiffContextFormatter
   {
      public DiffContextFormatter()
      {
         _css = loadStylesFromCSS();
      }

      public string FormatAsHTML(DiffContext context, int fontSizePx = 12, int rowsVPaddingPx = 2)
      {
         return getContextHTML(context, fontSizePx, rowsVPaddingPx);
      }

      private string getContextHTML(DiffContext ctx, int fontSizePx, int rowsVPaddingPx)
      {
         string customStyle = getCustomStyle(fontSizePx, rowsVPaddingPx);

         string commonBegin = string.Format(@"
            <html>
               <head>
                  <style>{0}{1}</style>
               </head>
               <body>
                  <table cellspacing=""0"" cellpadding=""0"">
                      <tbody>", _css, customStyle);

         string commonEnd = @"
                      </tbody>
                   </table>
                </body>
             </html>";

         return commonBegin + getTableBody(ctx) + commonEnd;
      }

      private string loadStylesFromCSS()
      {
         return Properties.Resources.DiffContextCSS;
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
         string body = string.Empty;
         for (int iLine = 0; iLine < ctx.Lines.Count; ++iLine)
         {
            var line = ctx.Lines[iLine];

            body
              += "<tr" + (iLine == ctx.SelectedIndex ? " class=\"selected\"" : "") + ">"
               + "<td class=\"linenumbers\">" + getLeftLineNumber(line) + "</td>" 
               + "<td class=\"linenumbers\">" + getRightLineNumber(line) + "</td>"
               + "<td class=\"" + getDiffCellClass(line) + "\">" + getCode(line) + "</td>"
               + "</tr>";
         }
         return body;
      }

      private string getLeftLineNumber(DiffContext.Line line)
      {
         return line.Left.HasValue ? line.Left.Value.Number.ToString() : "";
      }

      private string getRightLineNumber(DiffContext.Line line)
      {
         return line.Right.HasValue ? line.Right.Value.Number.ToString() : "";
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
         Debug.Assert(false);
         return "added";
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

         return spaces + trimmed;
      }

      private readonly string _css;
   }
}
