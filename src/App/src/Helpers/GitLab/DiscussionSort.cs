using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GitLabSharp.Entities;

namespace mrHelper.App.Helpers
{
   /// <summary>
   /// Possible choices of sorting discussion list
   /// </summary>
   public enum DiscussionSortState
   {
      Default,
      Reverse,
      ByReviewer
   };

   /// <summary>
   /// Sorts discussions
   /// </summary>
   public class DiscussionSort
   {
      public DiscussionSort(DiscussionSortState initialState)
      {
         SortState = initialState;
      }

      public DiscussionSortState SortState { get; set; }

      public IEnumerable<T> Sort<T>(IEnumerable<T> discussions, Func<T, IEnumerable<DiscussionNote>> fnGetNotes)
      {
         switch (SortState)
         {
            case DiscussionSortState.Default:
               return discussions;

            case DiscussionSortState.Reverse:
               return discussions.Reverse();

            case DiscussionSortState.ByReviewer:
               return discussions
                  .Where(x => fnGetNotes((dynamic)x).Count > 0)
                  .OrderBy(x => fnGetNotes((dynamic)x)[0].Author.Name);
         }

         Debug.Assert(false);
         return null;
      }
   }
}

