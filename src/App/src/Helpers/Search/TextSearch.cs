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
      public int InsideControlPosition;
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

      internal SearchResults<TextSearchResult> Search(string text, bool forward)
      {
         IEnumerable<Control> controls = getSearchableControls(_container);
         List<TextSearchResult> searchResults = new List<TextSearchResult>();

         foreach (Control control in controls)
         {
            int startPosition = 0;
            while (doesMatchText(control, text, startPosition, out int insideControlPosition))
            {
               searchResults.Add(new TextSearchResult
                  {
                     Control = control,
                     InsideControlPosition = insideControlPosition
                  });
               startPosition = insideControlPosition + 1;
            }
         }

         return new SearchResults<TextSearchResult>(searchResults.ToArray(), forward);
      }

      private bool doesMatchText(Control control, string text, int startPosition, out int insideControlPosition)
      {
         insideControlPosition = -1;

         int position = control.Text.IndexOf(text, startPosition);
         if (position != -1)
         {
            insideControlPosition = position;
            return true;
         }

         return false;
      }

      Control _container;
      Func<Control, bool> _isSearchableControl;
   }
}

