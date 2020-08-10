using GitLabSharp.Entities;
using System.Collections.Generic;

namespace mrHelper.GitLabClient
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

            public override bool Equals(object obj)
            {
               return obj is UpdateScope scope &&
                      Commits == scope.Commits &&
                      Labels == scope.Labels &&
                      Details == scope.Details;
            }

            public override int GetHashCode()
            {
               int hashCode = -87658868;
               hashCode = hashCode * -1521134295 + Commits.GetHashCode();
               hashCode = hashCode * -1521134295 + Labels.GetHashCode();
               hashCode = hashCode * -1521134295 + Details.GetHashCode();
               return hashCode;
            }
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

            public override bool Equals(object obj)
            {
               return obj is KeywordDescription description &&
                      Keyword == description.Keyword &&
                      EqualityComparer<User>.Default.Equals(Author, description.Author);
            }

            public override int GetHashCode()
            {
               int hashCode = -2089405254;
               hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Keyword);
               hashCode = hashCode * -1521134295 + EqualityComparer<User>.Default.GetHashCode(Author);
               return hashCode;
            }
         }

         public MergeRequestKey MergeRequestKey { get; }
         public Type EventType { get; }
         public object Details { get; }
      }
   }
}
