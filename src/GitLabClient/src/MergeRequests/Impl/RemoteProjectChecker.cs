using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using mrHelper.Client.Types;
using mrHelper.Client.Common;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;
using Version = GitLabSharp.Entities.Version;

namespace mrHelper.Client.MergeRequests
{
   /// <summary>
   /// Detects the latest change in a merge request by means of a request to GitLab
   /// </summary>
   public class RemoteProjectChecker : IInstantProjectChecker
   {
      internal RemoteProjectChecker(IEnumerable<Version> localVersions,
         MergeRequestKey mrk, UpdateOperator updateOperator)
      {
         _localVersions = localVersions;
         _mergeRequestKey = mrk;
         _operator = updateOperator;
      }

      /// <summary>
      /// Get a timestamp of the most recent change of a project the merge request belongs to
      /// Throws nothing
      /// </summary>
      async public Task<ProjectSnapshot> GetProjectSnapshot()
      {
         List<Version> allVersions = new List<Version>();
         allVersions.AddRange(_localVersions);

         try
         {
            IEnumerable<Version> remoteVersions = await _operator.GetVersionsAsync(_mergeRequestKey);
            allVersions.AddRange(remoteVersions);
         }
         catch (OperatorException ex)
         {
            ExceptionHandlers.Handle("Cannot obtain latest version for RemoteProjectChecker", ex);
         }

         List<string> shas = new List<string>();
         foreach (Version version in allVersions)
         {
            shas.Add(version.Base_Commit_SHA);
            shas.Add(version.Head_Commit_SHA);
         }

         return new ProjectSnapshot
         {
            LatestChange = allVersions.OrderBy(x => x.Created_At).LastOrDefault().Created_At,
            Sha = shas
         };
      }

      public override string ToString()
      {
         return String.Format("RemoteProjectChecker. MRK: HostName={0}, ProjectName={1}, IId={2}",
            _mergeRequestKey.ProjectKey.HostName, _mergeRequestKey.ProjectKey.ProjectName, _mergeRequestKey.IId);
      }

      private readonly IEnumerable<Version> _localVersions;
      private readonly MergeRequestKey _mergeRequestKey;

      private readonly UpdateOperator _operator;
   }
}

