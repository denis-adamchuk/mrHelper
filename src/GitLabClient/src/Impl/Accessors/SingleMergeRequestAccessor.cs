using System;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient.Accessors;
using mrHelper.GitLabClient.Interfaces;

namespace mrHelper.GitLabClient
{
   public class SingleMergeRequestAccessorException : ExceptionEx
   {
      internal SingleMergeRequestAccessorException(string message, Exception innerException)
         : base(message, innerException)
      {
      }
   }

   public class SingleMergeRequestAccessor
   {
      internal SingleMergeRequestAccessor(IHostProperties settings, MergeRequestKey mrk,
         IModificationListener modificationListener, INetworkOperationStatusListener networkOperationStatusListener)
      {
         _settings = settings;
         _mrk = mrk;
         _modificationListener = modificationListener;
         _networkOperationStatusListener = networkOperationStatusListener;
      }

      public IMergeRequestEditor GetMergeRequestEditor()
      {
         return new MergeRequestEditor(_settings, _mrk, _modificationListener, _networkOperationStatusListener);
      }

      public DiscussionAccessor GetDiscussionAccessor()
      {
         return new DiscussionAccessor(_settings, _mrk, _modificationListener, _networkOperationStatusListener);
      }

      public ITimeTracker GetTimeTracker()
      {
         return new TimeTracker(_mrk, _settings, _modificationListener, _networkOperationStatusListener);
      }

      private readonly MergeRequestKey _mrk;
      private readonly IHostProperties _settings;
      private readonly IModificationListener _modificationListener;
      private readonly INetworkOperationStatusListener _networkOperationStatusListener;
   }
}

