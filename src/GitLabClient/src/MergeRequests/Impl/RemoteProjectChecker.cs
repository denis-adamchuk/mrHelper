using System;
using System.Threading.Tasks;
using mrHelper.Client.Types;
using mrHelper.Client.Common;

namespace mrHelper.Client.MergeRequests
{
   /// <summary>
   /// Detects the latest change in a merge request by means of a request to GitLab
   /// </summary>
   public class RemoteProjectChecker : IInstantProjectChecker
   {
      internal RemoteProjectChecker(MergeRequestKey mrk, UpdateOperator updateOperator)
      {
         _mergeRequestKey = mrk;
         _operator = updateOperator;
      }

      /// <summary>
      /// Get a timestamp of the most recent change of a project the merge request belongs to
      /// Throws nothing
      /// </summary>
      async public Task<DateTime> GetLatestChangeTimestampAsync()
      {
         try
         {
            return (await _operator.GetLatestVersionAsync(_mergeRequestKey)).Created_At;
         }
         catch (OperatorException)
         {
            // already handled
         }
         return DateTime.MinValue;
      }

      public override string ToString()
      {
         return String.Format("RemoteProjectChecker. MRK: HostName={0}, ProjectName={1}, IId={2}",
            _mergeRequestKey.ProjectKey.HostName, _mergeRequestKey.ProjectKey.ProjectName, _mergeRequestKey.IId);
      }

      private MergeRequestKey _mergeRequestKey;
      private readonly UpdateOperator _operator;
   }
}

