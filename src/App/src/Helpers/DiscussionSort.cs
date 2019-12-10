using GitLabSharp.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrHelper.App.Helpers
{
   /// <summary>
   /// Possible choices of sorting discussion list
   /// </summary>
   public enum DiscussionSortState
   {
      Default,
      ByAuthor
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

      public IEnumerable<Discussion> Sort(IEnumerable<Discussion> discussions)
      {
         switch (SortState)
         {
            case DiscussionSortState.Default:
               return discussions;

            case DiscussionSortState.ByAuthor:
               return discussions
                  .Where(x => x.Notes.Count > 0)
                  .OrderByDescending(x => x.Notes[0].Author.Name);
         }

         Debug.Assert(false);
         return null;
      }
   }
}

