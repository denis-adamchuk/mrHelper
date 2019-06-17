using System;
using System.Text.RegularExpressions;

namespace mrHelper
{
   public struct ParsedMergeRequestUrl
   {
      public string Host;
      public string Project;
      public int Id;
   }

   class mrUrlParser
   {
      static Regex url_re = new Regex(
         @"^(http[s]?:\/)\/([^:\/\s]+)\/(\w+\/\w+)\/merge_requests\/(\d)(\/.*)?",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

      public mrUrlParser(string url)
      {
         _url = url;
      }

      public ParsedMergeRequestUrl Parse()
      {
         if (!Uri.IsWellFormedUriString(_url, UriKind.Absolute))
         {
            throw new UriFormatException("Wrong URL format");
         }

         Match m = url_re.Match(_url);
         if (!m.Success)
         {
            // TODO - Error handling
            throw new NotImplementedException("Failed");
         }

         if (m.Groups.Count < 5)
         {
            // TODO - Error handling
            throw new NotImplementedException("Unsupported merge requests URL format");
         }

         ParsedMergeRequestUrl result = new ParsedMergeRequestUrl();
         result.Host = m.Groups[2].Value;
         result.Project = m.Groups[3].Value;
         result.Id = int.Parse(m.Groups[4].Value);
         return result;
      }

      private readonly string _url;
   }
}
