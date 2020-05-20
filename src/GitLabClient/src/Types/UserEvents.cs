using GitLabSharp.Entities;

namespace mrHelper.Client.Types
{
   public static class UserEvents
   {
      public class MergeRequestEvent
      {
         public MergeRequestEvent(FullMergeRequestKey fullMergeRequestKey, Type eventType, object scope)
         {
            FullMergeRequestKey = fullMergeRequestKey;
            EventType = eventType;
            Scope = scope;
         }

         public enum Type
         {
            NewMergeRequest,
            UpdatedMergeRequest,
            ClosedMergeRequest
         }

         public struct UpdateScope
         {
            public UpdateScope(bool commits, bool labels, bool details)
            {
               Commits = commits;
               Labels = labels;
               Details = details;
            }

            public bool Commits { get; }
            public bool Labels { get; }
            public bool Details { get; }
         }

         public FullMergeRequestKey FullMergeRequestKey { get; }
         public Type EventType { get; }
         public object Scope { get; }

         public bool New => EventType == Type.NewMergeRequest;
         public bool Commits => EventType == Type.UpdatedMergeRequest && ((UpdateScope)(Scope)).Commits;
         public bool Labels => EventType == Type.UpdatedMergeRequest && ((UpdateScope)(Scope)).Labels;
         public bool Details => EventType == Type.UpdatedMergeRequest && ((UpdateScope)(Scope)).Details;
         public bool Closed => EventType == Type.ClosedMergeRequest;
      }

      public class DiscussionEvent
      {
         public DiscussionEvent(MergeRequestKey mergeRequestKey, Type eventType, object details)
         {
            MergeRequestKey = mergeRequestKey;
            EventType = eventType;
            Details = details;
         }

         public enum Type
         {
            ResolvedAllThreads,
            MentionedCurrentUser,
            Keyword
         }

         public class KeywordDescription
         {
            public KeywordDescription(string keyword, User author)
            {
               Keyword = keyword;
               Author = author;
            }

            public string Keyword { get; }
            public User Author { get; }
         }

         public MergeRequestKey MergeRequestKey { get; }
         public Type EventType { get; }
         public object Details { get; }
      }
   }
}
