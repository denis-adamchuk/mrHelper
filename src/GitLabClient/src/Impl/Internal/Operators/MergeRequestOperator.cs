using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitLabSharp.Accessors;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;

namespace mrHelper.GitLabClient.Operators
{
   internal class MergeRequestOperator : BaseOperator
   {
      internal MergeRequestOperator(string host, IHostProperties settings)
         : base(host, settings)
      {
      }

      async internal Task<IEnumerable<MergeRequest>> SearchMergeRequestsAsync(SearchCriteria searchCriteria)
      {
         List<MergeRequest> mergeRequests = new List<MergeRequest>();
         foreach (object search in searchCriteria.Criteria)
         {
            mergeRequests.AddRange(
               await callWithSharedClient(
                  async (client) =>
                     await OperatorCallWrapper.Call(
                        async () =>
                           await CommonOperator.SearchMergeRequestsAsync(client, searchCriteria))));
         }
         return mergeRequests;
      }

      internal Task<MergeRequest> CreateMergeRequest(string projectName, CreateNewMergeRequestParameters parameters)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     (MergeRequest)(await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.Get(projectName).MergeRequests.CreateNewTaskAsync(parameters)))));
      }

      internal Task<MergeRequest> UpdateMergeRequest(MergeRequestKey mrk, UpdateMergeRequestParameters parameters)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     (MergeRequest)(await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.Get(mrk.ProjectKey.ProjectName).MergeRequests.Get(mrk.IId)
                              .UpdateMergeRequestTaskAsync(parameters)))));
      }

      internal Task<MergeRequestRebaseResponse> RebaseMergeRequest(MergeRequestKey mrk, bool? skipCI)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     (MergeRequestRebaseResponse)(await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.Get(mrk.ProjectKey.ProjectName).MergeRequests.Get(mrk.IId)
                              .RebaseMergeRequestTaskAsync(skipCI)))));
      }

      internal Task<MergeRequest> AcceptMergeRequest(MergeRequestKey mrk, AcceptMergeRequestParameters parameters)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     (MergeRequest)(await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.Get(mrk.ProjectKey.ProjectName).MergeRequests.Get(mrk.IId)
                              .AcceptMergeRequestTaskAsync(parameters)))));
      }
   }
}

