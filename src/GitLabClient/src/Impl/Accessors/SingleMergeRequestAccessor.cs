using System;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient.Accessors;

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
         IModificationListener modificationListener)
      {
         _settings = settings;
         _mrk = mrk;
         _modificationListener = modificationListener;
      }

      public IMergeRequestEditor GetMergeRequestEditor()
      {
         return new MergeRequestEditor(_settings, _mrk, _modificationListener);
      }

      public DiscussionAccessor GetDiscussionAccessor()
      {
         return new DiscussionAccessor(_settings, _mrk, _modificationListener);
      }

      public ITimeTracker GetTimeTracker()
      {
         return new TimeTracker(_mrk, _settings, _modificationListener);
      }

      private readonly MergeRequestKey _mrk;
      private readonly IHostProperties _settings;
      private readonly IModificationListener _modificationListener;
   }
}

