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
         _settings = settings;
      }

      async public Task<IEnumerable<Version>> GetVersions(MergeRequestKey mrk)
      {
         _operator = new VersionOperator(mrk.ProjectKey.HostName,
            _settings.GetAccessToken(mrk.ProjectKey.HostName));
         try
         {
            return await _operator.LoadVersionsAsync(mrk);
         }
         catch (OperatorException ex)
         {
            if (ex.InnerException is GitLabSharp.GitLabClientCancelled)
            {
               return null;
            }
            throw new VersionManagerException("Cannot load versions", ex);
         }
      }

      async public Task<Version?> GetVersion(Version version, MergeRequestKey mrk)
      {
         _operator = new VersionOperator(mrk.ProjectKey.HostName,
            _settings.GetAccessToken(mrk.ProjectKey.HostName));
         try
         {
            return await _operator.LoadVersionAsync(version, mrk);
         }
         catch (OperatorException ex)
         {
            if (ex.InnerException is GitLabSharp.GitLabClientCancelled)
            {
               return null;
            }
            throw new VersionManagerException("Cannot load version", ex);
         }
      }

      async public Task CancelAsync()
      {
         if (_operator != null)
         {
            await _operator.CancelAsync();
         }
      }

      private readonly IHostProperties _settings;
      private VersionOperator _operator;
   }
}

