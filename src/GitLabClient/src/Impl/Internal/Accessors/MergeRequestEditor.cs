﻿using System;
using System.Threading.Tasks;
using GitLabSharp.Accessors;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient.Operators;

namespace mrHelper.GitLabClient.Accessors
{
   internal class MergeRequestEditor : IMergeRequestEditor
   {
      internal MergeRequestEditor(IHostProperties hostProperties,
            MergeRequestKey mrk, IModificationListener modificationListener)
      {
         _mrk = mrk;
         _hostProperties = hostProperties;
         _modificationListener = modificationListener;
      }

      async public Task ModifyMergeRequest(UpdateMergeRequestParameters parameters)
      {
         using (MergeRequestOperator mergeRequestOperator = new MergeRequestOperator(
            _mrk.ProjectKey.HostName, _hostProperties))
         {
            await mergeRequestOperator.UpdateMergeRequest(_mrk, parameters);
         }
      }

      async public Task AddTrackedTime(TimeSpan span, bool add)
      {
         try
         {
            using (TimeTrackingOperator timeTrackingOperator = new TimeTrackingOperator(
               _mrk.ProjectKey.HostName, _hostProperties))
            {
               await timeTrackingOperator.AddSpanAsync(true, span, _mrk);
               _modificationListener.OnTrackedTimeModified(_mrk, span, true);
            }
         }
         catch (OperatorException ex)
         {
            throw new TimeTrackingException("Cannot add a span", ex);
         }
      }

      private readonly MergeRequestKey _mrk;
      private readonly IHostProperties _hostProperties;
      private readonly IModificationListener _modificationListener;
   }
}

