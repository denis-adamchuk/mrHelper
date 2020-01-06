using GitLabSharp.Entities;
using mrHelper.Client.Types;

namespace mrHelper.Client.Common
{
   public static class UserEvents
   {
      public struct MergeRequestEvent
      {
         public enum Type
         {
            NewMergeRequest,
            UpdatedMergeRequest,
            ClosedMergeRequest
         }

         public struct UpdateScope
         {
            public bool Commits;
            public bool Labels;
            public bool Details;
         }

         public FullMergeRequestKey FullMergeRequestKey;
         public Type EventType;
         public object Scope;

         public bool New => EventType == Type.NewMergeRequest;
         public bool Commits => EventType == Type.UpdatedMergeRequest && ((UpdateScope)(Scope)).Commits;
         public bool Labels => EventType == Type.UpdatedMergeRequest && ((UpdateScope)(Scope)).Labels;
         public bool Details => EventType == Type.UpdatedMergeRequest && ((UpdateScope)(Scope)).Details;
         public bool Closed => EventType == Type.ClosedMergeRequest;
      }

      public struct DiscussionEvent
      {
         public enum Type
         {
            ResolvedAllThreads,
            MentionedCurrentUser,
            Keyword
         }

         public struct KeywordDescription
         {
            public string Keyword;
            public User Author;
         }

         public MergeRequestKey MergeRequestKey;
         public Type EventType;
         public object Details;
      }
   }
}
