using System;
using System.Threading.Tasks;
using GitLabSharp.Accessors;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient.Operators;

namespace mrHelper.GitLabClient.Accessors
{
   public class MergeRequestCreator : IMergeRequestCreator, IDisposable
   {
      internal MergeRequestCreator(string hostname, IHostProperties hostProperties)
      {
         _operator = new MergeRequestOperator(hostname, hostProperties);
      }

      public void Dispose()
      {
         _operator.Dispose();
      }

      public Task CreateMergeRequest(CreateNewMergeRequestParameters parameters)
      {
         return _operator.CreateMergeRequest(parameters);
      }

      private readonly MergeRequestOperator _operator;
   }
}

