using GitLabSharp.Accessors;
using GitLabSharp.Entities;
using mrHelper.Client.Common;
using mrHelper.Client.Types;
using mrHelper.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrHelper.Client.MergeRequests
{
   internal class MergeRequestOperator : BaseOperator
   {
      internal MergeRequestOperator(string host, IHostProperties settings)
         : base(host, settings)
      {
      }

      async internal Task<IEnumerable<MergeRequest>> SearchMergeRequestsAsync(
         SearchCriteria searchCriteria, int? maxResults, bool onlyOpen)
      {
         List<MergeRequest> mergeRequests = new List<MergeRequest>();
         foreach (object search in searchCriteria.Criteria)
         {
            mergeRequests.AddRange(
               await callWithSharedClient(
                  async (client) =>
                     await OperatorCallWrapper.Call(
                        async () =>
                           await CommonOperator.SearchMergeRequestsAsync(client, searchCriteria, maxResults, onlyOpen))));
         }
         return mergeRequests;
      }

      internal Task CreateMergeRequest(CreateNewMergeRequestParameters parameters)
      {
         throw new NotImplementedException();
      }
   }
}

