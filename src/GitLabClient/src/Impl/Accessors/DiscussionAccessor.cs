using System;
using GitLabSharp.Entities;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient.Accessors;
using mrHelper.GitLabClient.Interfaces;

namespace mrHelper.GitLabClient
{
   public class DiscussionAccessorException : ExceptionEx
   {
      internal DiscussionAccessorException(string message, Exception innerException)
         : base(message, innerException)
      {
      }
   }

   public class DiscussionAccessor
   {
      internal DiscussionAccessor(IHostProperties settings, MergeRequestKey mrk,
         IModificationListener modificationListener, IConnectionLossListener connectionLossListener)
      {
         _settings = settings;
         _mrk = mrk;
         _modificationListener = modificationListener;
         _connectionLossListener = connectionLossListener;
      }

      public DiscussionCreator GetDiscussionCreator(User user)
      {
         return new DiscussionCreator(_mrk, _settings, user, _connectionLossListener);
      }

      public SingleDiscussionAccessor GetSingleDiscussionAccessor(string discussionId)
      {
         return new SingleDiscussionAccessor(_settings, _mrk, discussionId, _modificationListener,
            _connectionLossListener);
      }

      private readonly IHostProperties _settings;
      private readonly MergeRequestKey _mrk;
      private readonly IModificationListener _modificationListener;
      private readonly IConnectionLossListener _connectionLossListener;
   }
}

