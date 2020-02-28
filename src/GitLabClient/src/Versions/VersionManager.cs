using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Version = GitLabSharp.Entities.Version;
using mrHelper.Client.Types;
using mrHelper.Common.Interfaces;
using mrHelper.Client.Common;
using mrHelper.Common.Exceptions;

namespace mrHelper.Client.Versions
{
   public class VersionManagerException : ExceptionEx
   {
      internal VersionManagerException(string message, Exception innerException)
         : base(message, innerException)
      {
      }
   }

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
         catch (OperatorException ex)
         {
            throw new VersionManagerException("Cannot load versions", ex);
         }
      }

      public Task<Version> GetVersion(Version version, MergeRequestKey mrk)
      {
         try
         {
            return _operator.LoadVersionAsync(version, mrk);
         }
         catch (OperatorException ex)
         {
            throw new VersionManagerException("Cannot load version", ex);
         }
      }

      private readonly VersionOperator _operator;
   }
}

