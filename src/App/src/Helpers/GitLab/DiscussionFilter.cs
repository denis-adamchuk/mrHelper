using System;
using System.Linq;
using GitLabSharp.Entities;

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
   /// Possible choices of filtering discussion list by resolved/not-resolved criteria
   /// </summary>
   [Flags]
   public enum FilterByResolution
   {
      Resolved   = 1,
      NotResolved = 2
   };

   /// <summary>
   /// Current state of discussion filter
   /// </summary>
   public struct DiscussionFilterState : IEquatable<DiscussionFilterState>
   {
      public DiscussionFilterState(bool byCurrentUserOnly, bool serviceMessages, bool systemNotes,
         FilterByAnswers byAnswers, FilterByResolution byResolution)
      {
         ByCurrentUserOnly = byCurrentUserOnly;
         ServiceMessages = serviceMessages;
         SystemNotes = systemNotes;
         ByAnswers = byAnswers;
         ByResolution = byResolution;
      }

      public bool ByCurrentUserOnly { get; }
      public bool ServiceMessages { get; }
      public bool SystemNotes { get; }
      public FilterByAnswers ByAnswers { get; }
      public FilterByResolution ByResolution { get; }

      static public DiscussionFilterState AllExceptSystem
      {
         get
         {
            return new DiscussionFilterState(false, true, false,
               FilterByAnswers.Answered | FilterByAnswers.Unanswered,
               FilterByResolution.Resolved | FilterByResolution.NotResolved);
         }
      }

      static public DiscussionFilterState Default
      {
         get
         {
            return new DiscussionFilterState(false, false, false,
               FilterByAnswers.Answered | FilterByAnswers.Unanswered,
               FilterByResolution.Resolved | FilterByResolution.NotResolved);
         }
      }

      static public DiscussionFilterState CurrentUserOnly
      {
         get
         {
            return new DiscussionFilterState(true, true, false,
               FilterByAnswers.Answered | FilterByAnswers.Unanswered,
               FilterByResolution.Resolved | FilterByResolution.NotResolved);
         }
      }

      public override bool Equals(object obj)
      {
         return obj is DiscussionFilterState state && Equals(state);
      }

      public bool Equals(DiscussionFilterState other)
      {
         return ByCurrentUserOnly == other.ByCurrentUserOnly &&
                ServiceMessages == other.ServiceMessages &&
                SystemNotes == other.SystemNotes &&
                ByAnswers == other.ByAnswers &&
                ByResolution == other.ByResolution;
      }

      public override int GetHashCode()
      {
         int hashCode = -858265698;
         hashCode = hashCode * -1521134295 + ByCurrentUserOnly.GetHashCode();
         hashCode = hashCode * -1521134295 + ServiceMessages.GetHashCode();
         hashCode = hashCode * -1521134295 + SystemNotes.GetHashCode();
         hashCode = hashCode * -1521134295 + ByAnswers.GetHashCode();
         hashCode = hashCode * -1521134295 + ByResolution.GetHashCode();
         return hashCode;
      }
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
         if (discussion.Notes.Count() == 0)
         {
            return false;
         }

         if (!Filter.SystemNotes && discussion.Notes.First().System)
         {
            return false;
         }

         if (Filter.ByCurrentUserOnly && discussion.Notes.First().Author.Id != CurrentUser.Id)
         {
            return false;
         }

         if (!Filter.ServiceMessages && isServiceDiscussioNote(discussion.Notes.First()))
         {
            return false;
         }

         if (MergeRequestAuthor != null)
         {
            bool isLastNoteFromMergeRequestAuthor = discussion.Notes.Last().Author.Id == MergeRequestAuthor.Id;
            bool matchByAnswers =
                   (Filter.ByAnswers.HasFlag(FilterByAnswers.Answered) && isLastNoteFromMergeRequestAuthor)
                || (Filter.ByAnswers.HasFlag(FilterByAnswers.Unanswered) && !isLastNoteFromMergeRequestAuthor);
            if (!matchByAnswers)
            {
               return false;
            }
         }

         bool isDiscussionResolved = discussion.Notes.Cast<DiscussionNote>().All(x => (!x.Resolvable || x.Resolved));
         bool matchByResolved =
                (Filter.ByResolution.HasFlag(FilterByResolution.Resolved) && isDiscussionResolved)
             || (Filter.ByResolution.HasFlag(FilterByResolution.NotResolved) && !isDiscussionResolved);
         if (!matchByResolved)
         {
            return false;
         }

         return true;
      }

      private static bool isServiceDiscussioNote(DiscussionNote note)
      {
         return note.Author.Username == Program.ServiceManager.GetServiceMessageUsername();
      }

      private User CurrentUser;
      private User MergeRequestAuthor;
   }
}

