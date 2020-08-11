using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;

namespace mrHelper.App.Helpers.GitLab
{
   public struct ParsedNewMergeRequestUrl : IEquatable<ParsedNewMergeRequestUrl>
   {
      public ParsedNewMergeRequestUrl(ProjectKey projectKey, string sourceBranch, string targetBranch)
      {
         ProjectKey = projectKey;
         SourceBranch = sourceBranch;
         TargetBranch = targetBranch;
      }

      public ProjectKey ProjectKey { get; }
      public string SourceBranch { get; }
      public string TargetBranch { get; }

      public override bool Equals(object obj)
      {
         return obj is ParsedNewMergeRequestUrl url && Equals(url);
      }

      public bool Equals(ParsedNewMergeRequestUrl other)
      {
         return ProjectKey.Equals(other.ProjectKey) &&
                SourceBranch == other.SourceBranch &&
                TargetBranch == other.TargetBranch;
      }

      public override int GetHashCode()
      {
         int hashCode = 741791702;
         hashCode = hashCode * -1521134295 + ProjectKey.GetHashCode();
         hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(SourceBranch);
         hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TargetBranch);
         return hashCode;
      }
   }

   /// <summary>
   /// <summary>
   public static class NewMergeRequestUrlParser
   {
      public static readonly string RegEx =
         @"create\/mr" +
         @"\?(?:path\=(?'path'.*))" +
         @"\&(?:source_branch\=(?'source_branch'[\w_\-\/]+))";

      private static readonly Regex url_re = new Regex(RegEx, RegexOptions.Compiled | RegexOptions.IgnoreCase);

      /// <summary>
      /// Splits passed url in parts and stores in object properties
      /// <summary>
      public static ParsedNewMergeRequestUrl Parse(string url)
      {
         Match m = url_re.Match(url);
         if (!m.Success)
         {
            throw new UriFormatException("Failed to parse URL");
         }

         Group path = m.Groups["path"];
         Group sourceBranch = m.Groups["source_branch"];
         if (!path.Success || !sourceBranch.Success)
         {
            throw new UriFormatException("Unsupported URL format");
         }

         ProjectKey? projectKey = GitTools.GetRepositoryProjectKey(path.Value);
         if (!projectKey.HasValue)
         {
            throw new UriFormatException(String.Format("\"{0}\" is not a git repository", path.Value));
         }

         string targetBranch = findTargetBranch(path.Value, sourceBranch.Value);
         return new ParsedNewMergeRequestUrl(projectKey.Value, sourceBranch.Value, targetBranch);
      }

      private static string findTargetBranch(string path, string sourceBranch)
      {
         // TODO
         return "master";
      }
   }
}

