using System;
using System.Collections.Generic;
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
      public DiscussionFilterState(bool byCurrentUserOnly, bool serviceMessages,
         FilterByAnswers byAnswers, FilterByResolution byResolution, IEnumerable<Discussion> enabledDiscussions)
      {
         ByCurrentUserOnly = byCurrentUserOnly;
         ServiceMessages = serviceMessages;
         ByAnswers = byAnswers;
         ByResolution = byResolution;
         EnabledDiscussions = enabledDiscussions;
      }

      public DiscussionFilterState(IEnumerable<Discussion> enabledDiscussions)
      {
         ByCurrentUserOnly = Default.ByCurrentUserOnly;
         ServiceMessages = Default.ServiceMessages;
         ByAnswers = Default.ByAnswers;
         ByResolution = Default.ByResolution;
         EnabledDiscussions = enabledDiscussions;
      }

      public bool ByCurrentUserOnly { get; }
      public bool ServiceMessages { get; }
      public FilterByAnswers ByAnswers { get; }
      public FilterByResolution ByResolution { get; }
      public IEnumerable<Discussion> EnabledDiscussions { get; }

      static public DiscussionFilterState Default
      {
         get
         {
            return new DiscussionFilterState(false, false,
               FilterByAnswers.Answered | FilterByAnswers.Unanswered,
               FilterByResolution.Resolved | FilterByResolution.NotResolved, null);
         }
      }

      static public DiscussionFilterState CurrentUserOnly
      {
         get
         {
            return new DiscussionFilterState(true, true,
               FilterByAnswers.Answered | FilterByAnswers.Unanswered,
               FilterByResolution.Resolved | FilterByResolution.NotResolved, null);
         }
      }

      public override bool Equals(object obj)
      {
         return obj is DiscussionFilterState state && Equals(state);
      }

      public bool Equals(DiscussionFilterState other)
      {
         // TODO Is this method needed?
         return ByCurrentUserOnly == other.ByCurrentUserOnly &&
                ServiceMessages == other.ServiceMessages &&
                ByAnswers == other.ByAnswers &&
                ByResolution == other.ByResolution &&
                EnabledDiscussions.SequenceEqual(other.EnabledDiscussions);
      }

      public override int GetHashCode()
      {
         // TODO Is this method needed?
         int hashCode = -858265698;
         hashCode = hashCode * -1521134295 + ByCurrentUserOnly.GetHashCode();
         hashCode = hashCode * -1521134295 + ServiceMessages.GetHashCode();
         hashCode = hashCode * -1521134295 + ByAnswers.GetHashCode();
         hashCode = hashCode * -1521134295 + ByResolution.GetHashCode();
         hashCode = hashCode * -1521134295 + EnabledDiscussions.GetHashCode();
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
         _filterState = initialState;
      }

      public event Action FilterStateChanged;

      public DiscussionFilterState FilterState
      {
         get => _filterState;
         set
         {
            _filterState = value;
            FilterStateChanged?.Invoke();
         }
      }

      public bool DoesMatchFilter(Discussion discussion)
      {
         if (_filterState.EnabledDiscussions != null
         && !_filterState.EnabledDiscussions.Select(x => x.Id).Contains(discussion.Id))
         {
            return false;
         }

         if (discussion.Notes.Count() == 0)
         {
            return false;
         }

         if (_filterState.ByCurrentUserOnly && discussion.Notes.First().Author.Id != CurrentUser.Id)
         {
            return false;
         }

         if (!_filterState.ServiceMessages && isServiceDiscussioNote(discussion.Notes.First()))
         {
            return false;
         }

         if (MergeRequestAuthor != null)
         {
            bool isLastNoteFromMergeRequestAuthor = discussion.Notes.Last().Author.Id == MergeRequestAuthor.Id;
            bool matchByAnswers =
                   (_filterState.ByAnswers.HasFlag(FilterByAnswers.Answered) && isLastNoteFromMergeRequestAuthor)
                || (_filterState.ByAnswers.HasFlag(FilterByAnswers.Unanswered) && !isLastNoteFromMergeRequestAuthor);
            if (!matchByAnswers)
            {
               return false;
            }
         }

         bool isDiscussionResolved = discussion.Notes.Cast<DiscussionNote>().All(x => (!x.Resolvable || x.Resolved));
         bool matchByResolved =
                (_filterState.ByResolution.HasFlag(FilterByResolution.Resolved) && isDiscussionResolved)
             || (_filterState.ByResolution.HasFlag(FilterByResolution.NotResolved) && !isDiscussionResolved);
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

      private readonly User CurrentUser;
      private readonly User MergeRequestAuthor;
      private DiscussionFilterState _filterState;
   }
}

