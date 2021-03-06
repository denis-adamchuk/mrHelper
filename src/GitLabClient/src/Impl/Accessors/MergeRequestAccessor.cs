﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient.Operators;
using mrHelper.Common.Exceptions;
using mrHelper.GitLabClient.Accessors;

namespace mrHelper.GitLabClient
{
   public class MergeRequestAccessorException : ExceptionEx
   {
      internal MergeRequestAccessorException(string message, Exception innerException)
         : base(message, innerException)
      {
      }
   }

   public class MergeRequestAccessorCancelledException : MergeRequestAccessorException
   {
      internal MergeRequestAccessorCancelledException()
         : base(String.Empty, null) {}
   }

   public class MergeRequestAccessor
   {
      internal MergeRequestAccessor(IHostProperties settings, ProjectKey projectKey,
         IModificationListener modificationListener, INetworkOperationStatusListener networkOperationStatusListener)
      {
         _settings = settings;
         _projectKey = projectKey;
         _modificationListener = modificationListener;
         _networkOperationStatusListener = networkOperationStatusListener;
      }

      async public Task<MergeRequest> SearchMergeRequestAsync(int mergeRequestIId, bool onlyOpen)
      {
         using (MergeRequestOperator mergeRequestOperator = new MergeRequestOperator(
            _projectKey.HostName, _settings, _networkOperationStatusListener))
         {
            try
            {
               SearchQuery query = new SearchQuery
               {
                  IId = mergeRequestIId,
                  ProjectName = _projectKey.ProjectName,
                  State = onlyOpen ? "opened" : null,
                  MaxResults = 1
               };
               IEnumerable<MergeRequest> mergeRequests = await mergeRequestOperator.SearchMergeRequestsAsync(query);
               return mergeRequests.Any() ? mergeRequests.First() : null;
            }
            catch (OperatorException ex)
            {
               if (ex.Cancelled)
               {
                  throw new MergeRequestAccessorCancelledException();
               }
               throw new MergeRequestAccessorException("Merge request search failed", ex);
            }
         }
      }

      public MergeRequestCreator GetMergeRequestCreator()
      {
         return new MergeRequestCreator(_projectKey, _settings, _networkOperationStatusListener);
      }

      public SingleMergeRequestAccessor GetSingleMergeRequestAccessor(int iid)
      {
         return new SingleMergeRequestAccessor(_settings, new MergeRequestKey(_projectKey, iid),
            _modificationListener, _networkOperationStatusListener);
      }

      private readonly IHostProperties _settings;
      private readonly ProjectKey _projectKey;
      private readonly IModificationListener _modificationListener;
      private readonly INetworkOperationStatusListener _networkOperationStatusListener;
   }
}

