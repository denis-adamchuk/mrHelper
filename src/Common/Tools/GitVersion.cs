using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using mrHelper.Common.Exceptions;

namespace mrHelper.Common.Tools
{
   internal class GitVersion
   {
      internal static GitVersion FromText(string text)
      {
         Match m = GitVersionRegex.Match(text);
         if (!m.Success
          ||  m.Groups.Count < 3
          || !m.Groups["major"].Success
          || !m.Groups["minor"].Success
          || !m.Groups["build"].Success
          || !int.TryParse(m.Groups["major"].Value, out int major)
          || !int.TryParse(m.Groups["minor"].Value, out int minor)
          || !int.TryParse(m.Groups["build"].Value, out int build))
         {
            throw new ArgumentException();
         }
         return new GitVersion(major, minor, build);
      }

      internal Version ToVersion()
      {
         return new Version(_major, _minor, _build);
      }

      private GitVersion(int major, int minor, int build)
      {
         _major = major;
         _minor = minor;
         _build = build;
      }

      private static readonly string gitVersionFormat = @"(?'major'\d+).(?'minor'\d+).(?'build'\d+)";
      private static readonly Regex GitVersionRegex = new Regex(gitVersionFormat);

      private readonly int _major;
      private readonly int _minor;
      private readonly int _build;
   }

   public static class GitVersionCommandLineReader
   {
      public class UnknownVersionException : ExceptionEx
      {
         internal UnknownVersionException(Exception innerException)
            : base(String.Empty, innerException)
         {
         }
      }

      public static Version GetVersion()
      {
         if (_cachedVersion == null)
         {
            _cachedVersion = getVersion();
         }
         return _cachedVersion;
      }

      private static Version _cachedVersion;

      private static Version getVersion()
      {
         string cmdLineOutput;
         try
         {
            IEnumerable<string> stdOut =
               ExternalProcess.Start("git", "--version", true, String.Empty).StdOut;
            if (!stdOut.Any())
            {
               throw new UnknownVersionException(null);
            }

            cmdLineOutput = stdOut.First();
         }
         catch (ExternalProcessException ex)
         {
            throw new UnknownVersionException(ex);
         }

         string cmdLineVersionPrefix = "git version";
         string versionAsText = cmdLineOutput.StartsWith(cmdLineVersionPrefix) ?
            cmdLineOutput.Substring(cmdLineVersionPrefix.Length) : null;
         try
         {
            return GitVersion.FromText(versionAsText).ToVersion();
         }
         catch (ArgumentException ex)
         {
            throw new UnknownVersionException(ex);
         }
      }
   }
}

