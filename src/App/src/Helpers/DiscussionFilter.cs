using GitLabSharp.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrHelper.App.Helpers
{
   /// <summary>
   /// Possible choices of filtering discussion list by answered/unanswered criteria
   /// </summary>
   [Flags]
   public enum FilterByAnswers
   {
      Answered   = 1,
      Unanswered = 2
   };

   /// <summary>
   /// Current state of discussion filter
   /// </summary>
   public struct DiscussionFilterState
   {
      public bool ByCurrentUserOnly;
      public FilterByAnswers ByAnswers;
   }

   /// <summary>
   /// Filters out discussions that don't match specific criteria
   /// </summary>
   public class DiscussionFilter
   {
      public DiscussionFilter(User currentUser, User mergeRequestAuthor, DiscussionFilterState initialState)
      {
         CurrentUser = currentUser;
         MergeRequestAuthor = mergeRequestAuthor;
         Filter = initialState;
      }

      public DiscussionFilterState Filter { get; set; }

      public bool DoesMatchFilter(Discussion discussion)
      {
         if (discussion.Notes.Count == 0 || discussion.Notes[0].System)
         {
            return false;
         }
         
         if (Filter.ByCurrentUserOnly && discussion.Notes[0].Author.Id != CurrentUser.Id)
         {
            return false;
         }

         bool isLastNoteFromMergeRequestAuthor =
            discussion.Notes[discussion.Notes.Count - 1].Author.Id == MergeRequestAuthor.Id;
         return (Filter.ByAnswers.HasFlag(FilterByAnswers.Answered) && isLastNoteFromMergeRequestAuthor)
             || (Filter.ByAnswers.HasFlag(FilterByAnswers.Unanswered) && !isLastNoteFromMergeRequestAuthor);
      }

      private User CurrentUser;
      private User MergeRequestAuthor;
   }
}

