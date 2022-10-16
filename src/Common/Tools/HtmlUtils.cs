using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace mrHelper.Common.Tools
{
   public static class HtmlUtils
   {
      private const string open_token_PRE = "<pre";
      private const string close_token_PRE = "</pre>";
      private const string open_code_token = "<code>";
      private const string close_code_token = "</code>";
      private const string image_token = "<img";

      public static string WrapImageIntoTables(string html)
      {
         return forEachUnaryToken(html, image_token,
            (result, iFirstCharOfOpenToken, iFirstCharOfCloseToken) =>
         {
            string prefix = "<table><tr><td>";
            string suffix = "</td></tr></table>";
            if (iFirstCharOfOpenToken - prefix.Length >= 0
             && result.Substring(iFirstCharOfOpenToken - prefix.Length, prefix.Length) == prefix
             && iFirstCharOfCloseToken + 2 + suffix.Length < result.Length
             && result.Substring(iFirstCharOfCloseToken + 2, suffix.Length) == suffix)
            {
               return result;
            }
            return String.Format("{0}{1}{2}{3}{4}",
               result.Substring(0, iFirstCharOfOpenToken),
               prefix,
               result.Substring(iFirstCharOfOpenToken, iFirstCharOfCloseToken - iFirstCharOfOpenToken + 2),
               suffix,
               result.Substring(iFirstCharOfCloseToken + 2));
         });
      }

      /// <summary>
      /// For "<pre><code>something</code></pre>" calculates width of "something" in pixels
      /// </summary>
      public static int? CalcMaximumPreElementWidth(string html, Func<string, int> fnWidthCalculator)
      {
         int? maxWidth = new int?();
         forEachToken(html, open_token_PRE, close_token_PRE,
            (result, iNextCharAfterOpenToken, iFirstCharOfMatchingCloseToken) =>
         {
            while (iNextCharAfterOpenToken < html.Length && html[iNextCharAfterOpenToken] != open_code_token[0])
            {
               ++iNextCharAfterOpenToken;
            }
            int? width = getWidthInsidePRE(result, iNextCharAfterOpenToken, iFirstCharOfMatchingCloseToken,
               fnWidthCalculator);
            if (width.HasValue)
            {
               maxWidth = maxWidth.HasValue ? Math.Max(maxWidth.Value, width.Value) : width;
            }
            return result;
         });
         return maxWidth;
      }

      /// <summary>
      /// Adds "width: Npx" to <pre> of "<pre><code>something</code></pre>" where N is width of "something"
      /// </summary>
      public static string AddWidthAttributeToCodeElements(string body, Func<string, int> fnWidthCalculator)
      {
         return forEachToken(body, open_token_PRE, close_token_PRE,
            (result, iNextCharAfterOpenToken, iFirstCharOfMatchingCloseToken) =>
         {
            if (iNextCharAfterOpenToken >= result.Length - 1 || result[iNextCharAfterOpenToken] != '>')
            {
               return result; // ignore <pre> which are <pre ...>
            }
            ++iNextCharAfterOpenToken;
            int? width = getWidthInsidePRE(result, iNextCharAfterOpenToken, iFirstCharOfMatchingCloseToken,
               fnWidthCalculator);
            string style = width.HasValue ? String.Format(" style=\"width:{0}px;\"", width) : String.Empty;
            return result.Insert(iNextCharAfterOpenToken - 1, style); // -1 to insert before '<'
         });
      }

      private static int? getWidthInsidePRE(string html, int iNextCharAfterOpenPre, int iFirstCharOfClosePre,
         Func<string, int> fnWidthCalculator)
      {
         if (iFirstCharOfClosePre - iNextCharAfterOpenPre < open_code_token.Length + close_code_token.Length)
         {
            return new int?();
         }

         int iFirstChartOfOpenCode = iNextCharAfterOpenPre;
         int iFirstCharOfCloseCode = iFirstCharOfClosePre - close_code_token.Length;

         bool areCodeTagsBetweenPreTags =
               html.Substring(iFirstChartOfOpenCode, open_code_token.Length) == open_code_token
            && html.Substring(iFirstCharOfCloseCode, close_code_token.Length) == close_code_token;

         iFirstChartOfOpenCode += open_code_token.Length;
         if (areCodeTagsBetweenPreTags)
         {
            int maxWidthInPixels = html
               .Substring(iFirstChartOfOpenCode, iFirstCharOfCloseCode - iFirstChartOfOpenCode)
               .Split('\n')
               .Select(substr => System.Net.WebUtility.HtmlDecode(substr))
               .Select(substr => fnWidthCalculator(substr))
               .Max();
            return maxWidthInPixels;
         }
         return new int?();
      }

      // TODO This algorithm is pretty inefficient and can be optimized.
      private static string forEachToken(string body, string openToken, string closeToken,
         Func<string, int, int, string> fn)
      {
         if (!body.Contains(openToken))
         {
            return body;
         }

         List<int> getTokenPositions(string token, string text)
         {
            List<int> positions = new List<int>();
            void addPosition(int i)
            {
               if (i != -1)
               {
                  positions.Add(i);
               }
            }

            int iPosition = text.IndexOf(token, 0);
            addPosition(iPosition);
            while (iPosition != -1)
            {
               iPosition = text.IndexOf(token, iPosition + 1);
               addPosition(iPosition);
            }

            return positions;
         }

         List<int> openTokens = getTokenPositions(openToken, body);
         List<int> closeTokens = getTokenPositions(closeToken, body);
         if (openTokens.Count != closeTokens.Count)
         {
            return body;
         }

         int iOpenToken = 0;
         int iCloseToken = 0;
         string result = body;
         while (true)
         {
            // skip nested tokens
            int iMatchingCloseToken = iCloseToken;
            int iNextOpenToken = iOpenToken + 1;
            while (iNextOpenToken < openTokens.Count
                && openTokens[iNextOpenToken] > openTokens[iOpenToken]
                && openTokens[iNextOpenToken] < closeTokens[iMatchingCloseToken])
            {
               ++iMatchingCloseToken;
               ++iNextOpenToken;
            }

            // apply fn
            int iNextCharAfterOpenToken = openTokens[iOpenToken] + openToken.Length;
            int iFirstCharOfMatchingCloseToken = closeTokens[iMatchingCloseToken];
            result = fn(result, iNextCharAfterOpenToken, iFirstCharOfMatchingCloseToken);

            // recalculate indices
            List<int> openTokensNew = getTokenPositions(openToken, result);
            List<int> closeTokensNew = getTokenPositions(closeToken, result);

            // check if we need to start from scratch or stop
            if (!openTokensNew.SequenceEqual(openTokens) || !closeTokensNew.SequenceEqual(closeTokens))
            {
               if (openTokensNew.Count != closeTokensNew.Count
                || openTokensNew.Count == 0
                || iOpenToken == openTokensNew.Count - 1)
               {
                  break;
               }
               iOpenToken = 0;
               iCloseToken = 0;
               openTokens = openTokensNew;
               closeTokens = closeTokensNew;
               continue;
            }

            // prepare to next iteration
            iOpenToken = iOpenToken + (iMatchingCloseToken - iCloseToken) + 1;
            iCloseToken = iMatchingCloseToken + 1;
            if (iOpenToken > openTokens.Count - 1)
            {
               break;
            }
         }

         return result;
      }

      // TODO This algorithm is pretty inefficient and can be optimized.
      private static string forEachUnaryToken(string body, string openToken,
         Func<string, int, int, string> fn)
      {
         if (!body.Contains(openToken))
         {
            return body;
         }

         List<Tuple<int, int>> getTokenPositions(string token, string text)
         {
            List<Tuple<int, int>> positions = new List<Tuple<int, int>>();
            void addPosition(int i, int j)
            {
               if (i != -1 && j != -1)
               {
                  positions.Add(new Tuple<int, int>(i, j));
               }
            }

            int iPosition = text.IndexOf(token, 0);
            int jPosition = text.IndexOf("/>", iPosition + 1);
            addPosition(iPosition, jPosition);
            while (iPosition != -1 && jPosition != -1)
            {
               iPosition = text.IndexOf(token, jPosition + 1);
               jPosition = text.IndexOf("/>", iPosition + 1);
               addPosition(iPosition, jPosition);
            }

            return positions;
         }

         List<Tuple<int, int>> tokenPositions = getTokenPositions(openToken, body);

         int iCurrentPosition = 0;
         string result = body;
         while (true)
         {
            // apply fn
            int iFirstCharOfOpenToken = tokenPositions[iCurrentPosition].Item1;
            int iFirstCharOfCloseToken = tokenPositions[iCurrentPosition].Item2;
            result = fn(result, iFirstCharOfOpenToken, iFirstCharOfCloseToken);

            // recalculate indices
            List<Tuple<int, int>> tokenPositionsNew = getTokenPositions(openToken, result);

            // check if we need to start from scratch or stop
            if (!tokenPositionsNew.SequenceEqual(tokenPositions))
            {
               if (tokenPositionsNew.Count == 0 || iCurrentPosition == tokenPositionsNew.Count - 1)
               {
                  break;
               }
               iCurrentPosition = 0;
               tokenPositions = tokenPositionsNew;
               continue;
            }

            // prepare to next iteration
            iCurrentPosition++;
            if (iCurrentPosition > tokenPositions.Count - 1)
            {
               break;
            }
         }

         return result;
      }

      public static void Test_CalcWidthAttributeToCodeElements()
      {
         int calcWidth(string s) => s.Length;

         // empty
         Debug.Assert(null == CalcMaximumPreElementWidth("", calcWidth));
         Debug.Assert(null == CalcMaximumPreElementWidth("0", calcWidth));
         Debug.Assert(null == CalcMaximumPreElementWidth("<pre></pre>", calcWidth));
         Debug.Assert(0 == CalcMaximumPreElementWidth("1<pre><code></code></pre>1", calcWidth));

         // 1
         Debug.Assert(1 == CalcMaximumPreElementWidth("<pre><code>1</code></pre>", calcWidth));

         // 2
         Debug.Assert(2 == CalcMaximumPreElementWidth("1<pre><code>12</code></pre>1", calcWidth));

         // two subsequent elements
         Debug.Assert(3 == CalcMaximumPreElementWidth("<pre><code>12</code></pre>3456<pre><code>123</code></pre>", calcWidth));

         // nested
         Debug.Assert(28 == CalcMaximumPreElementWidth("<pre><code>1<pre><code>23</code>4</pre></code></pre>", calcWidth));

         Debug.Assert(39 == CalcMaximumPreElementWidth("<pre><code>1<pre><code><pre>23</pre></code>4</pre></code></pre>", calcWidth));

         Debug.Assert(50 == CalcMaximumPreElementWidth("ABC<pre><code>1<pre><code><pre>23</pre></code></pre><pre>4</pre></code></pre>ABC", calcWidth));

         // nested and non-nested
         Debug.Assert(60 == CalcMaximumPreElementWidth("ABC<pre><code>1<pre><code><pre>23</pre></code></pre><pre>4</pre></code></pre>" +
                                                       "ABC<pre><code>012345678901234567890123456789012345678901234567890123456789</code></pre>DEF", calcWidth));

         // no <code> or </code>
         Debug.Assert(null == CalcMaximumPreElementWidth("<pre>123</pre>", calcWidth));
         Debug.Assert(null == CalcMaximumPreElementWidth("<pre><code>123</pre>", calcWidth)); //-V3050

         // bad
         Debug.Assert(null == CalcMaximumPreElementWidth("<pre>123", calcWidth));
         Debug.Assert(null == CalcMaximumPreElementWidth("<pre><code>1<pre><code>2<pre><code>3</code></pre>4<code>5</pre></code>6</code></pre>7</code></pre>", calcWidth)); //-V3050
         Debug.Assert(null == CalcMaximumPreElementWidth("<pre><code>1<pre><code>2<code>3</code></pre>4<pre><code>5</pre></code>6</code></pre>7</code></pre>", calcWidth)); //-V3050
      }

      public static void Test_AddWidthAttributeToCodeElements()
      {
         int calcWidth(string s) => s.Length;

         // empty
         Debug.Assert(                         ""
            == AddWidthAttributeToCodeElements("", calcWidth));
         Debug.Assert(                         "0"
            == AddWidthAttributeToCodeElements("0", calcWidth));
         Debug.Assert(                         "<pre></pre>"
            == AddWidthAttributeToCodeElements("<pre></pre>", calcWidth));
         Debug.Assert(                         "1<pre style=\"width:0px;\"><code></code></pre>1"
            == AddWidthAttributeToCodeElements("1<pre><code></code></pre>1", calcWidth));

         // 1
         Debug.Assert(                         "<pre style=\"width:1px;\"><code>1</code></pre>"
            == AddWidthAttributeToCodeElements("<pre><code>1</code></pre>", calcWidth));

         // 2
         Debug.Assert(                         "1<pre style=\"width:2px;\"><code>12</code></pre>1" 
            == AddWidthAttributeToCodeElements("1<pre><code>12</code></pre>1", calcWidth));

         // two subsequent elements
         Debug.Assert(                         "<pre style=\"width:2px;\"><code>12</code></pre>3456<pre style=\"width:3px;\"><code>123</code></pre>" 
            == AddWidthAttributeToCodeElements("<pre><code>12</code></pre>3456<pre><code>123</code></pre>", calcWidth));

         // nested
         Debug.Assert(                      "<pre style=\"width:28px;\"><code>1<pre><code>23</code>4</pre></code></pre>" ==
            AddWidthAttributeToCodeElements("<pre><code>1<pre><code>23</code>4</pre></code></pre>", calcWidth));

         Debug.Assert(                      "<pre style=\"width:39px;\"><code>1<pre><code><pre>23</pre></code>4</pre></code></pre>" ==
            AddWidthAttributeToCodeElements("<pre><code>1<pre><code><pre>23</pre></code>4</pre></code></pre>", calcWidth));

         Debug.Assert(                      "ABC<pre style=\"width:50px;\"><code>1<pre><code><pre>23</pre></code></pre><pre>4</pre></code></pre>ABC" ==
            AddWidthAttributeToCodeElements("ABC<pre><code>1<pre><code><pre>23</pre></code></pre><pre>4</pre></code></pre>ABC", calcWidth));

         // nested and non-nested
         Debug.Assert(                      "ABC<pre style=\"width:50px;\"><code>1<pre><code><pre>23</pre></code></pre><pre>4</pre></code></pre>ABC<pre style=\"width:3px;\"><code>123</code></pre>DEF" ==
            AddWidthAttributeToCodeElements("ABC<pre><code>1<pre><code><pre>23</pre></code></pre><pre>4</pre></code></pre>ABC<pre><code>123</code></pre>DEF", calcWidth));

         // no <code> or </code>
         Debug.Assert("<pre>123</pre>" == AddWidthAttributeToCodeElements("<pre>123</pre>", calcWidth));
         Debug.Assert("<pre><code>123</pre>" == AddWidthAttributeToCodeElements("<pre><code>123</pre>", calcWidth)); //-V3050

         // bad
         Debug.Assert("<pre>123" == AddWidthAttributeToCodeElements("<pre>123", calcWidth));
         Debug.Assert(                      "<pre><code>1<pre><code>2<pre><code>3</code></pre>4<code>5</pre></code>6</code></pre>7</code></pre>" == //-V3050
            AddWidthAttributeToCodeElements("<pre><code>1<pre><code>2<pre><code>3</code></pre>4<code>5</pre></code>6</code></pre>7</code></pre>", calcWidth)); //-V3050
         Debug.Assert(                      "<pre><code>1<pre><code>2<code>3</code></pre>4<pre><code>5</pre></code>6</code></pre>7</code></pre>" == //-V3050
            AddWidthAttributeToCodeElements("<pre><code>1<pre><code>2<code>3</code></pre>4<pre><code>5</pre></code>6</code></pre>7</code></pre>", calcWidth)); //-V3050
      }

      public static void Test_WrapImageIntoTables()
      {
         // empty
         Debug.Assert(                         ""
            == WrapImageIntoTables(""));

         // one
         Debug.Assert("<table><tr><td><img src=\"abc\"/></td></tr></table>"
            == WrapImageIntoTables("<img src=\"abc\"/>"));

         // one in <p>
         Debug.Assert("<p><table><tr><td><img src=\"UntitledImage.png\"/></td></tr></table></p>" == WrapImageIntoTables("<p><img src=\"UntitledImage.png\"/></p>"));

         // two
         Debug.Assert("<table><tr><td><img src=\"UntitledImage.png\"/></td></tr></table>def<table><tr><td><img src=\"ghi\"/></td></tr></table>"
            == WrapImageIntoTables("<img src=\"UntitledImage.png\"/>def<img src=\"ghi\"/>"));
      }
   }
}

