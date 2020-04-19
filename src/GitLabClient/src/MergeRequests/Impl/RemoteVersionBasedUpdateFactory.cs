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
   /// Detects the latest change among given versions, including remote versions for a given merge request
   /// </summary>
   public class RemoteVersionBasedUpdateFactory : IProjectUpdateFactory
   {
      internal RemoteVersionBasedUpdateFactory(IEnumerable<Version> localVersions,
         MergeRequestKey mrk, UpdateOperator updateOperator)
      {
         _localVersions = localVersions;
         _mergeRequestKey = mrk;
         _operator = updateOperator;
      }

      async public Task<IProjectUpdate> GetUpdate()
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

         return new FullProjectUpdate
         {
            LatestChange = allVersions.OrderBy(x => x.Created_At).LastOrDefault().Created_At,
            Sha = shas
         };
      }

      public override string ToString()
      {
         return String.Format("RemoteVersionBasedUpdateFactory. MRK: HostName={0}, ProjectName={1}, IId={2}",
            _mergeRequestKey.ProjectKey.HostName, _mergeRequestKey.ProjectKey.ProjectName, _mergeRequestKey.IId);
      }

      private readonly IEnumerable<Version> _localVersions;
      private readonly MergeRequestKey _mergeRequestKey;

      private readonly UpdateOperator _operator;
   }
}

