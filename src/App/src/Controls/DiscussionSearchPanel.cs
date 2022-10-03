using System;
using System.Windows.Forms;
using mrHelper.App.Helpers;

namespace mrHelper.App.Controls
{
   public partial class DiscussionSearchPanel : UserControl
   {
      public DiscussionSearchPanel()
      {
         InitializeComponent();
         labelFoundCount.Visible = false;
         enableButtons();
      }

      internal void Initialize(ITextControlHost host)
      {
         _host = host;
         _host.ContentChanged += onHostContentChanged;
      }

      internal void ProcessKeyDown(KeyEventArgs e)
      {
         if (e.KeyCode == Keys.F && e.Modifiers.HasFlag(Keys.Control))
         {
            Focus();
            textBoxSearch.SelectAll();
            e.Handled = true;
         }
         else if (e.KeyCode == Keys.F3)
         {
            bool isShitPressed = e.Modifiers.HasFlag(Keys.Shift);
            continueSearch(isShitPressed ? SearchDirection.Backward : SearchDirection.Forward);
            e.Handled = true;
         }
         else if (e.KeyCode == Keys.Escape)
         {
            resetSearch();
            e.Handled = true;
         }
      }

      private void ButtonFind_Click(object sender, EventArgs e)
      {
         onSearchButton(SearchDirection.Forward);
      }

      private void buttonFindPrev_Click(object sender, EventArgs e)
      {
         onSearchButton(SearchDirection.Backward);
      }

      private void textBoxSearch_TextChanged(object sender, EventArgs e)
      {
         enableButtons();
         resetSearch();
      }

      private void textBoxSearch_KeyDown(object sender, KeyEventArgs e)
      {
         if (e.KeyCode == Keys.Enter)
         {
            buttonFindNext.PerformClick();
         }
      }

      private void checkBoxCaseSensitive_CheckedChanged(object sender, EventArgs e)
      {
         buttonFindNext.PerformClick();
      }

      private void checkBoxShowFoundOnly_CheckedChanged(object sender, EventArgs e)
      {
         restartSearch();
      }

      private void onHostContentChanged()
      {
         restartSearch();
      }

      private enum SearchDirection
      {
         Forward,
         Backward
      }

      private void onSearchButton(SearchDirection searchDirection)
      {
         SearchQuery query = new SearchQuery(textBoxSearch.Text, checkBoxCaseSensitive.Checked);
         if (query.Text == String.Empty)
         {
            resetSearch();
         }
         else if (_textSearch == null || !query.Equals(_textSearch.Query))
         {
            startSearch(query, true);
         }
         else
         {
            continueSearch(searchDirection);
         }
      }

      private void displayFoundCount(int? count)
      {
         labelFoundCount.Visible = count.HasValue;
         labelFoundCount.Text = count.HasValue ? String.Format(
            "Found {0} results. {1}", count.Value,
            count.Value > 0
            ? "Use F3/Shift-F3 to navigate between search results." : String.Empty): String.Empty;
      }

      private void enableButtons()
      {
         bool hasText = textBoxSearch.Text.Length > 0;
         buttonFindNext.Enabled = hasText;
         buttonFindPrev.Enabled = hasText;
         checkBoxShowFoundOnly.Enabled = hasText;
      }

      private void highlightSearchResult(TextSearchResult? result)
      {
         _textSearchResult = null;
         if (result.HasValue && _textSearch != null)
         {
            result.Value.Control.HighlightFragment(result.Value.InsideControlPosition, _textSearch.Query.Text.Length);
            _textSearchResult = result;
         }
      }

      private void startSearch(SearchQuery query, bool highlight)
      {
         resetSearch();

         _textSearch = new TextSearch(_host.Controls, query);
         TextSearchResult? result = _textSearch.FindFirst(out int count);
         displayFoundCount(count);

         if (highlight)
         {
            highlightSearchResult(result);
         }

         _host.OnSearchResults(_textSearch.FindAll(), checkBoxShowFoundOnly.Checked);
      }

      private void restartSearch()
      {
         if (_textSearch != null)
         {
            startSearch(_textSearch.Query, false);
         }
      }

      private void continueSearch(SearchDirection searchButton)
      {
         if (_textSearch == null)
         {
            return;
         }

         int startPosition = 0;
         ITextControl control = _host.ActiveControl;
         if (control != null && control.HighlightState != null)
         {
            startPosition = searchButton == SearchDirection.Forward
               ? control.HighlightState.HighlightStart + control.HighlightState.HighlightLength
               : control.HighlightState.HighlightStart ;
            control.ClearHighlight();
         }

         TextSearchResult? result = searchButton == SearchDirection.Forward
            ? _textSearch.FindNext(control, startPosition)
            : _textSearch.FindPrev(control, startPosition);

         if (result != null)
         {
            highlightSearchResult(result);
         }
      }

      private void resetSearch()
      {
         _textSearch = null;
         displayFoundCount(null);
         _textSearchResult?.Control.ClearHighlight();
         _textSearchResult = null;
         _host.OnSearchResults(null, false);
      }

      private TextSearch _textSearch;
      private TextSearchResult? _textSearchResult;
      private ITextControlHost _host;
   }
}

