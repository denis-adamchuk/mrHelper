using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Version = GitLabSharp.Entities.Version;
using mrHelper.Client.Types;
using mrHelper.Common.Interfaces;
using mrHelper.Client.Common;

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
         try
         {
            return _operator.LoadVersionsAsync(mrk);
         }
         catch (OperatorException)
         {
            throw new VersionManagerException();
         }
      }

      public Task<Version> GetVersion(Version version, MergeRequestKey mrk)
      {
         try
         {
            return _operator.LoadVersionAsync(version, mrk);
         }
         catch (OperatorException)
         {
            throw new VersionManagerException();
         }
      }

      private readonly VersionOperator _operator;
   }
}

