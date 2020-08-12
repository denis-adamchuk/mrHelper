using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using mrHelper.Common.Constants;
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
         @"\?(?:Repository\=(?'Repository'.*))" +
         @"\&(?:SourceBranch\=(?'SourceBranch'[\w_\-\/]+))";

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

         Group path = m.Groups["Repository"];
         Group sourceBranch = m.Groups["SourceBranch"];
         if (!path.Success || !sourceBranch.Success)
         {
            throw new UriFormatException("Unsupported URL format");
         }

         ProjectKey? projectKey = GitTools.GetRepositoryProjectKey(path.Value);
         if (!projectKey.HasValue)
         {
            throw new UriFormatException(String.Format("\"{0}\" is not a git repository", path.Value));
         }

         // sourceBranch can be one of the following:
         // - br_foo
         // - origin/br_foo
         // - 53ff02a
         // Resolve all these cases to origin/br_foo here.
         string remoteSourceBranch = getRemoteSourceBranch(path.Value, sourceBranch.Value);
         if (String.IsNullOrEmpty(remoteSourceBranch))
         {
            throw new UriFormatException(String.Format("\"{0}\" does not point to a remote branch", sourceBranch.Value));
         }

         string remoteTargetBranch = findTargetBranch(path.Value, remoteSourceBranch);

         string remoteOrigin = "origin/";
         Debug.Assert(remoteSourceBranch.StartsWith(remoteOrigin));
         Debug.Assert(remoteTargetBranch.StartsWith(remoteOrigin));
         string sourceBranchName = remoteSourceBranch.Substring(remoteOrigin.Length);
         string targetBranchName = remoteTargetBranch.Substring(remoteOrigin.Length);
         return new ParsedNewMergeRequestUrl(projectKey.Value, sourceBranchName, targetBranchName);
      }

      private static string getRemoteSourceBranch(string path, string sourceBranch)
      {
         IEnumerable<string> refs = GitTools.GetRemotePointsAt(path, sourceBranch);
         return refs != null ? refs.FirstOrDefault() : null;
      }

      private static string findTargetBranch(string path, string remoteSourceBranch)
      {
         for (int iDepth = 0; iDepth < Constants.MaxCommitDepth; ++iDepth)
         {
            string sha = String.Format("{0}{1}", remoteSourceBranch, new string('^', iDepth));
            IEnumerable<string> refs = GitTools.GetRemotePointsAt(path, sha).Where(x => x != remoteSourceBranch);
            if (refs != null && refs.Any())
            {
               return refs.First();
            }
         }
         return "origin/master";
      }
   }
}

