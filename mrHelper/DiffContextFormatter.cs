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
                      <tbody>
                         <tr>", loadStylesFromCSS());

         string commonEnd = @"
                         </tr>
                      </tbody>
                   </table>
                </body>
             </html>";

         return commonBegin + getLineNumbersColumn(ctx) + getTextDiffColumn(ctx) + commonEnd;
      }

      private string loadStylesFromCSS()
      {
         return mrHelperUI.Properties.Resources.DiffContextCSS;
      }

      private string getLineNumbersColumn(DiffContext ctx)
      {
         return "<td class=\"linenumbers\">" + enumerateLines(ctx.Lines, (x) => { return getSides(x); }) + "</td>";
      }

      private string getTextDiffColumn(DiffContext ctx)
      {
         return "<td class=\"text-diff\">" + enumerateLines(ctx.Lines, (x) => { return getCode(x); }) + "</td>";
      }

      private string getSides(DiffContext.Line line)
      {
         if (line.Left.HasValue && line.Right.HasValue)
         {
            return "<div class=\"unchanged-linenumber-left\">" + line.Left.Value.Number.ToString() + "</div>" +
                   "<div class=\"unchanged-linenumber-right\">" + line.Right.Value.Number.ToString() + "</div>";
         }
         else if (line.Left.HasValue)
         {
            return "<div class=\"removed-linenumber\">" + line.Left.Value.Number.ToString() + "</div>" +
                   "<div class=\"dummy-linenumber-right\"></div>";

         }
         else if (line.Right.HasValue)
         {
            return "<div class=\"dummy-linenumber-left\"></div>" +
                   "<div class=\"added-linenumber\">" + line.Right.Value.Number.ToString() + "</div>";
         }
         Debug.Assert(false);
         return "";
      }

      private string getCode(DiffContext.Line line)
      {
         string text = line.Text.Length == 0 ? "<br>" : line.Text;
         if (line.Left.HasValue && line.Right.HasValue)
         {
            return "<div class=\"unchanged-line\">" + text + "</div>";
         }
         else if (line.Left.HasValue)
         {
            if (line.Left.Value.State == DiffContext.Line.State.Removed)
            {
               return "<div class=\"removed-line\">" + text + "</div>";
            }
            else
            {
               Debug.Assert(line.Left.Value.State == DiffContext.Line.State.Unchanged);
               return "<div class=\"unchanged-line\">" + text + "</div>";
            }
         }
         else if (line.Right.HasValue)
         {
            if (line.Right.Value.State == DiffContext.Line.State.Added)
            {
               return "<div class=\"added-line\">" + text + "</div>";
            }
            else
            {
               Debug.Assert(line.Right.Value.State == DiffContext.Line.State.Unchanged);
               return "<div class=\"unchanged-line\">" + text + "</div>";
            }
         }
         Debug.Assert(false);
         return "";
      }

      private delegate string ActionOnLine(DiffContext.Line line);

      private string enumerateLines(List<DiffContext.Line> lines, ActionOnLine action)
      {
         string result = string.Empty;
         for (int iLine = 0; iLine < lines.Count; ++iLine)
         {
            result += action(lines[iLine]);
         }
         return result;
      }
   }
}
