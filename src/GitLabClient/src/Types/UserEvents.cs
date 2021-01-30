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
            AddedMergeRequest,
            UpdatedMergeRequest,
            RemovedMergeRequest
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

         public bool AddedToCache => EventType == Type.AddedMergeRequest;
         public bool Commits => EventType == Type.UpdatedMergeRequest && ((UpdateScope)(Scope)).Commits;
         public bool Labels => EventType == Type.UpdatedMergeRequest && ((UpdateScope)(Scope)).Labels;
         public bool Details => EventType == Type.UpdatedMergeRequest && ((UpdateScope)(Scope)).Details;
         public bool RemovedFromCache => EventType == Type.RemovedMergeRequest;
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
            Keyword,
            ApprovalStatusChange
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
                      (Author?.Id ?? 0) == (description.Author?.Id ?? 0);
            }

            public override int GetHashCode()
            {
               int hashCode = -2089405254;
               hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Keyword);
               hashCode = hashCode * -1521134295 + EqualityComparer<User>.Default.GetHashCode(Author);
               return hashCode;
            }
         }

         public class ApprovalStatusChangeDescription
         {
            public ApprovalStatusChangeDescription(bool isApproved, User author)
            {
               IsApproved = isApproved;
               Author = author;
            }

            public bool IsApproved { get; }
            public User Author { get; }
         }

         public MergeRequestKey MergeRequestKey { get; }
         public Type EventType { get; }
         public object Details { get; }
      }
   }
}
