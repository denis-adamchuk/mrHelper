using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace mrHelper.Common.Tools
{
   /// <summary>
   /// Splits merge request url in parts and stores them in properties
   /// <summary>
   public static class UrlParser
   {
      private static readonly string mergeRequestUrlRegExText =
         @"(http[s]?:\/\/)?([^:\/\s]+)\/(api\/v4\/projects\/)?([\.\w_-]+\/[\.\w_-]+)\/(?>-\/)?merge_requests\/(\d*)";
      private static readonly Regex mergeRequestUrlRegEx = new Regex(mergeRequestUrlRegExText, RegexOptions.Compiled | RegexOptions.IgnoreCase);

      private static readonly string noteUrlRegExText = mergeRequestUrlRegExText + @"#note_(?'note_id'\d*)";
      private static readonly Regex noteUrlRegEx = new Regex(noteUrlRegExText, RegexOptions.Compiled | RegexOptions.IgnoreCase);

      static readonly int MaxUrlLength = 256;

      public struct ParsedMergeRequestUrl : IEquatable<ParsedMergeRequestUrl>
      {
         public ParsedMergeRequestUrl(string host, string project, int iid)
         {
            Host = host;
            Project = project;
            IId = iid;
         }

         public string Host { get; }
         public string Project { get; }
         public int IId { get; }

         public override bool Equals(object obj)
         {
            return obj is ParsedMergeRequestUrl url && Equals(url);
         }

         public bool Equals(ParsedMergeRequestUrl other)
         {
            return Host == other.Host &&
                   Project == other.Project &&
                   IId == other.IId;
         }

         public override int GetHashCode()
         {
            int hashCode = 741791702;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Host);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Project);
            hashCode = hashCode * -1521134295 + IId.GetHashCode();
            return hashCode;
         }
      }

      public struct ParsedNoteUrl
      {
         public ParsedNoteUrl(string host, string project, int iid, int noteId)
         {
            _mergeRequestUrl = new ParsedMergeRequestUrl(host, project, iid);
            _noteId = noteId;
         }

         public string Host => _mergeRequestUrl.Host;
         public string Project => _mergeRequestUrl.Project;
         public int IId => _mergeRequestUrl.IId;
         public int NoteId => _noteId;

         private ParsedMergeRequestUrl _mergeRequestUrl;
         private int _noteId;
      }

      /// <summary>
      /// Splits passed url in parts and stores in object properties
      /// <summary>
      public static ParsedMergeRequestUrl ParseMergeRequestUrl(string url)
      {
         if (url.Length > MaxUrlLength)
         {
            throw new UriFormatException("Too long URL");
         }

         Match m = mergeRequestUrlRegEx.Match(url);
         if (!ifMergeRequestUrlMatchSucceeded(m))
         {
            throw new UriFormatException("Failed to parse URL");
         }
         return new ParsedMergeRequestUrl(m.Groups[2].Value, m.Groups[4].Value, int.Parse(m.Groups[5].Value));
      }

      public static ParsedNoteUrl ParseNoteUrl(string url)
      {
         if (url.Length > MaxUrlLength)
         {
            throw new UriFormatException("Too long URL");
         }

         Match m = noteUrlRegEx.Match(url);
         if (!ifNoteUrlMatchSucceeded(m))
         {
            throw new UriFormatException("Failed to parse URL");
         }
         return new ParsedNoteUrl(m.Groups[2].Value, m.Groups[4].Value, int.Parse(m.Groups[5].Value),
            int.Parse(m.Groups[6].Value));
      }

      public static bool IsValidMergeRequestUrl(string url)
      {
         if (url.Length > MaxUrlLength)
         {
            return false;
         }

         Match m = mergeRequestUrlRegEx.Match(url);
         return ifMergeRequestUrlMatchSucceeded(m);
      }

      public static bool IsValidNoteUrl(string url)
      {
         if (url.Length > MaxUrlLength)
         {
            return false;
         }

         Match m = noteUrlRegEx.Match(url);
         return ifNoteUrlMatchSucceeded(m);
      }

      private static bool ifMergeRequestUrlMatchSucceeded(Match m)
      {
         return m.Success
             && m.Groups.Count == 6
             && int.TryParse(m.Groups[5].Value, out _);
      }

      private static bool ifNoteUrlMatchSucceeded(Match m)
      {
         return m.Success
             && m.Groups.Count == 7
             && int.TryParse(m.Groups[5].Value, out _)
             && m.Groups["note_id"] != null
             && int.TryParse(m.Groups["note_id"].Value, out _);
      }
   }
}

