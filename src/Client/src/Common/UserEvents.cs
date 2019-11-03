using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;
using mrHelper.Client.Updates;

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

         public MergeRequestKey MergeRequestKey;
         public Type EventType;
         public object Details;
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
