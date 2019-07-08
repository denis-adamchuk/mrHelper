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
      }

      public string FormatAsHTML(DiffContext context)
      {
         return getContextHTML(context);
      }

      private string getContextHTML(DiffContext ctx)
      {
         string commonBegin = string.Format(@"
            <html>
               <head>
                  <style>{0}</style>
               </head>
               <body>
                  <table>
                      <tbody>", loadStylesFromCSS());

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

      private string getTableBody(DiffContext ctx)
      {
         string body = string.Empty;
         foreach (DiffContext.Line line in ctx.Lines)
         {
            body
              += "<tr class=\"" + getRowClass(line) + "\">"
               + getLeftLineNumberCol(line) 
               + getRightLineNumberCol(line) 
               + getTextDiffCol(line) 
               + "</tr>";
         }
         return body;
      }

      private string getLeftLineNumberCol(DiffContext.Line line)
      {
         return "<td class=\"linenumbers\">" + getLeftLineNumber(line) + "</td>";
      }

      private string getRightLineNumberCol(DiffContext.Line line)
      {
         return "<td class=\"linenumbers\">" + getRightLineNumber(line) + "</td>";
      }

      private string getTextDiffCol(DiffContext.Line line)
      {
         return "<td>" + getCode(line) + "</td>";
      }

      private string getRowClass(DiffContext.Line line)
      {
         if (line.Left.HasValue && line.Right.HasValue)
         {
            return "unchanged";
         }
         else if (line.Left.HasValue)
         {
            return "removed";
         }
         return "added";
      }

      private string getLeftLineNumber(DiffContext.Line line)
      {
         if (line.Left.HasValue && line.Right.HasValue)
         {
            return line.Left.Value.Number.ToString();
         }
         else if (line.Left.HasValue)
         {
            return line.Left.Value.Number.ToString();

         }
         return "";
      }

      private string getRightLineNumber(DiffContext.Line line)
      {
         if (line.Left.HasValue && line.Right.HasValue)
         {
            return line.Right.Value.Number.ToString();
         }
         else if (line.Right.HasValue)
         {
            return line.Right.Value.Number.ToString();
         }
         return "";
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
   }
}
