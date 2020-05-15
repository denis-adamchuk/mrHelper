using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using mrHelper.Client.Types;
using mrHelper.Client.Common;
using mrHelper.Client.Session;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;
using Version = GitLabSharp.Entities.Version;

namespace mrHelper.Client.MergeRequests
{
   /// <summary>
   /// Detects the latest change among given versions, including remote versions for a given merge request
   /// </summary>
   internal class RemoteBasedContextProvider : IProjectUpdateContextProvider
   {
      internal RemoteBasedContextProvider(IEnumerable<Version> localVersions,
         MergeRequestKey mrk, SessionOperator op)
      {
         _localVersions = localVersions;
         _mergeRequestKey = mrk;
         _operator = op;
      }

      async public Task<IProjectUpdateContext> GetContext()
      {
         List<Version> allVersions = new List<Version>();
         allVersions.AddRange(_localVersions);

         try
         {
            IEnumerable<Version> remoteVersions = await _operator.GetVersionsAsync(
               _mergeRequestKey.ProjectKey.ProjectName, _mergeRequestKey.IId);
            allVersions.AddRange(remoteVersions);
         }
         catch (OperatorException ex)
         {
            ExceptionHandlers.Handle("Cannot obtain latest version for RemoteBasedUpdateProvider", ex);
         }

         List<string> shas = new List<string>();
         foreach (Version version in allVersions)
         {
            shas.Add(version.Base_Commit_SHA);
            shas.Add(version.Head_Commit_SHA);
         }

         return new FullUpdateContext(allVersions.OrderBy(x => x.Created_At).LastOrDefault().Created_At, shas);
      }

      public override string ToString()
      {
         return String.Format("RemoteBasedUpdateProvider. MRK: HostName={0}, ProjectName={1}, IId={2}",
            _mergeRequestKey.ProjectKey.HostName, _mergeRequestKey.ProjectKey.ProjectName, _mergeRequestKey.IId);
      }

      private readonly SessionOperator _operator;
      private readonly IEnumerable<Version> _localVersions;
      private readonly MergeRequestKey _mergeRequestKey;
   }
}

