using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using mrHelper.Common.Constants;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;

namespace mrHelper.App.Helpers
{
   public struct ParsedNewMergeRequestUrl
   {
      public ParsedNewMergeRequestUrl(ProjectKey projectKey, string sourceBranch,
         IEnumerable<string> targetBranchCandidates)
      {
         ProjectKey = projectKey;
         SourceBranch = sourceBranch;
         TargetBranchCandidates = targetBranchCandidates;
      }

      public ProjectKey ProjectKey { get; }
      public string SourceBranch { get; }
      public IEnumerable<string> TargetBranchCandidates { get; }
   }

   /// <summary>
   /// <summary>
   public static class NewMergeRequestUrlParser
   {
      public static readonly string RegEx =
         @"create\/mr" +
         @"\?(?:Repository\=(?'Repository'.*))" +
         @"\&(?:SourceBranch\=(?'SourceBranch'[\w_\-\/]+))";

      static readonly int MaxUrlLength = 512;

      private static readonly Regex url_re = new Regex(RegEx, RegexOptions.Compiled | RegexOptions.IgnoreCase);

      /// <summary>
      /// Splits passed url in parts and stores in object properties
      /// <summary>
      public static ParsedNewMergeRequestUrl Parse(string url, Dictionary<string, string> sourceBranchTemplates)
      {
         if (url.Length > MaxUrlLength)
         {
            throw new UriFormatException("Too long URL");
         }

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
         sourceBranchTemplates.TryGetValue(projectKey.Value.HostName, out string templateForHost);
         string remoteSourceBranch = getRemoteSourceBranch(path.Value, sourceBranch.Value, templateForHost);
         if (String.IsNullOrEmpty(remoteSourceBranch))
         {
            throw new UriFormatException(String.Format("\"{0}\" does not point to a remote branch", sourceBranch.Value));
         }

         string sourceBranchName = trimRemoteOrigin(remoteSourceBranch);
         IEnumerable<string> targetBranchName = findTargetBranch(path.Value, remoteSourceBranch)
            .Select(fullName => trimRemoteOrigin(fullName));
         return new ParsedNewMergeRequestUrl(projectKey.Value, sourceBranchName, targetBranchName);
      }

      private static string getRemoteSourceBranch(string path, string sourceBranch, string template)
      {
         IEnumerable<string> refs = GitTools.GetRemotePointsAt(path, sourceBranch);
         if (refs == null)
         {
            return null;
         }
         if (!String.IsNullOrWhiteSpace(template))
         {
            Regex template_re = new Regex(template, RegexOptions.IgnoreCase);
            foreach (string candidate in refs)
            {
               Match m = template_re.Match(trimRemoteOrigin(candidate));
               if (m.Success)
               {
                  return candidate;
               }
            }
         }
         return refs.First();
      }

      private static IEnumerable<string> findTargetBranch(string path, string remoteSourceBranch)
      {
         for (int iDepth = 0; iDepth < Constants.MaxCommitDepth; ++iDepth)
         {
            string sha = String.Format("{0}{1}", remoteSourceBranch, new string('^', iDepth));
            IEnumerable<string> refs = GitTools.GetRemotePointsAt(path, sha)
               .Where(x => x != remoteSourceBranch)
               .Where(x => x != String.Format("{0}HEAD", RemoteOrigin));
            if (refs.Any())
            {
               return refs;
            }
         }
         return new string[] { String.Format("{0}master", RemoteOrigin) };
      }

      private static string trimRemoteOrigin(string name)
      {
         Debug.Assert(name.StartsWith(RemoteOrigin));
         return name.Substring(RemoteOrigin.Length);
      }
      private static readonly string RemoteOrigin = "origin/";
   }
}

