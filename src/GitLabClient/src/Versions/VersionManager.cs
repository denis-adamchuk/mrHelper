using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Version = GitLabSharp.Entities.Version;
using mrHelper.Client.Types;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.Versions
{
   public class VersionManagerException : Exception {}

   public class VersionManager
   {
      public VersionManager(IHostProperties settings)
      {
         _operator = new VersionOperator(settings);
      }

      public Task<IEnumerable<Version>> GetVersions(MergeRequestKey mrk)
      {
         return _operator.LoadVersionsAsync(mrk);
      }

      public Task<Version> GetVersion(Version version, MergeRequestKey mrk)
      {
         return _operator.LoadVersionAsync(version, mrk);
      }

      public Task<Version> GetLatestVersion(MergeRequestKey mrk)
      {
         return _operator.GetLatestVersionAsync(mrk);
      }

      private readonly VersionOperator _operator;
   }
}

