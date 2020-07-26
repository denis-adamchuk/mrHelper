using System.Collections.Generic;
using System.Threading.Tasks;
using GitLabSharp.Accessors;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.MergeRequests
{
   internal class MergeRequestCreator : IMergeRequestCreator
   {
      internal MergeRequestCreator(MergeRequestOperator mergeRequestOperator)
      {
         _operator = mergeRequestOperator;
      }

      public Task CreateMergeRequest(CreateNewMergeRequestParameters parameters)
      {
         return _operator.CreateMergeRequest(parameters);
      }

      private readonly MergeRequestOperator _operator;
   }
}

