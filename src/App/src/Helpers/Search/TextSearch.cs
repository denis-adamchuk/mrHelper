using mrHelper.App.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace mrHelper.App.Helpers
{
   internal struct TextSearchResult
   {
      public Control Control;
      public int InsideControlIndex;
      public int InsideControlPosition;
   }

   internal struct SearchQuery
   {
      public string Text;
      public bool CaseSensitive;
   }

   internal class TextSearch
   {
      internal TextSearch(Control container, Func<Control, bool> isSearchableControl)
      {
         _container = container;
         _isSearchableControl = isSearchableControl;
      }

      private List<Control> getSearchableControls(Control container)
      {
         List<Control> controlList = new List<Control>();
         foreach (Control control in container.Controls)
         {
            controlList.AddRange(getSearchableControls(control));
            if (_isSearchableControl(control))
            {
               controlList.Add(control);
            }
         }
         return controlList;
      }

      internal SearchResults<TextSearchResult> Search(SearchQuery query)
      {
         IEnumerable<Control> controls = getSearchableControls(_container);
         List<TextSearchResult> searchResults = new List<TextSearchResult>();

         foreach (Control control in controls)
         {
            int startPosition = 0;
            int insideControlIndex = 0;
            while (doesMatchText(control, query, startPosition, out int insideControlPosition))
            {
               searchResults.Add(new TextSearchResult
                  {
                     Control = control,
                     InsideControlIndex = insideControlIndex,
                     InsideControlPosition = insideControlPosition
                  });
               startPosition = insideControlPosition + 1;
               insideControlIndex++;
            }
         }

         return new SearchResults<TextSearchResult>(searchResults.ToArray());
      }

      private bool doesMatchText(Control control, SearchQuery query, int startPosition, out int insideControlPosition)
      {
         insideControlPosition = -1;

         StringComparison stringComparison = query.CaseSensitive ?
            StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
         int position = control.Text.IndexOf(query.Text, startPosition, stringComparison);
         if (position != -1)
         {
            insideControlPosition = position;
            return true;
         }

         return false;
      }

      readonly Control _container;
      readonly Func<Control, bool> _isSearchableControl;
   }
}

