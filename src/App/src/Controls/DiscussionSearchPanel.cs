using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

      internal void Initialize(ITextControlHost host,
         Action<IEnumerable<TextSearchResult>> onSearchResult)
      {
         _host = host;
         _onSearchResult = onSearchResult;
      }

      internal bool NeedShowFoundOnly()
      {
         Debug.Assert(checkBoxShowFoundOnly.CheckState != CheckState.Indeterminate);
         return checkBoxShowFoundOnly.Checked;
      }

      internal void RestartSearch()
      {
         restartSearch(true);
         resolveIndeterminateState();
      }

      internal void RefreshSearch()
      {
         int? foundCountOld = _resultCounter?.ControlCount;
         restartSearch(false);
         int? foundCountNew = _resultCounter?.ControlCount;
         if (foundCountOld > foundCountNew)
         {
            setIndeterminateState();
         }
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
            resolveIndeterminateState();
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

      private void checkBoxShowFoundOnly_Click(object sender, EventArgs e)
      {
         switch (checkBoxShowFoundOnly.CheckState)
         {
            case CheckState.Unchecked:
               checkBoxShowFoundOnly.CheckState = CheckState.Checked;
               break;

            case CheckState.Checked:
               checkBoxShowFoundOnly.CheckState = CheckState.Unchecked;
               break;

            default:
               Debug.Assert(checkBoxShowFoundOnly.CheckState == CheckState.Indeterminate);
               resolveIndeterminateState();
               break;
         }

         restartSearch(true);
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
            resolveIndeterminateState();
            resetSearch();
         }
         else if (_textSearch == null || !query.Equals(_textSearch.Query))
         {
            resolveIndeterminateState();
            startSearch(query, true, true);
         }
         else
         {
            continueSearch(searchDirection);
         }
      }

      private void setIndeterminateState()
      {
         if (checkBoxShowFoundOnly.CheckState == CheckState.Checked)
         {
            checkBoxShowFoundOnly.CheckState = CheckState.Indeterminate;
         }
      }

      private void resolveIndeterminateState()
      {
         if (checkBoxShowFoundOnly.CheckState == CheckState.Indeterminate)
         {
            checkBoxShowFoundOnly.CheckState = CheckState.Checked;
         }
      }

      private void displayFoundCount()
      {
         int? count = _resultCounter?.TotalCount;
         labelFoundCount.Visible = count.HasValue;
         labelFoundCount.Text = count.HasValue ? String.Format(
            "Found {0} result{2}. {1}", count.Value,
            count.Value > 1
            ? "Use F3/Shift-F3 to navigate between search results." : String.Empty,
            count.Value == 1 ? "" : "s"): String.Empty;
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
         if (result.HasValue && _textSearch != null && result.Value.Control != null)
         {
            result.Value.Control.HighlightFragment(result.Value.InsideControlPosition, _textSearch.Query.Text.Length);
            _textSearchResult = result;
         }
      }

      private void startSearch(SearchQuery query, bool needHighlightSearchResult, bool needTriggerCallback)
      {
         resetSearch(needTriggerCallback: false /* optimization */);

         _textSearch = new TextSearch(_host.Controls, query);
         TextSearchResult? result = _textSearch.FindFirst(out int totalCount, out int controlCount);
         _resultCounter = new ResultCounter(totalCount, controlCount);
         displayFoundCount();

         if (needTriggerCallback)
         {
            _onSearchResult?.Invoke(_textSearch.FindAll());
         }

         if (needHighlightSearchResult)
         {
            highlightSearchResult(result);
         }
      }

      private void restartSearch(bool needTriggerCallback)
      {
         if (_textSearch != null)
         {
            startSearch(_textSearch.Query, false, needTriggerCallback);
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

      private void resetSearch(bool needTriggerCallback = true)
      {
         if (_textSearch != null)
         {
            _textSearch = null;
            _resultCounter = null;
            displayFoundCount();
            _textSearchResult?.Control.ClearHighlight();
            _textSearchResult = null;
            if (needTriggerCallback)
            {
               _onSearchResult(null);
            }
         }
      }

      private TextSearch _textSearch;
      private TextSearchResult? _textSearchResult;
      private ITextControlHost _host;
      private Action<IEnumerable<TextSearchResult>> _onSearchResult;

      private struct ResultCounter
      {
         public ResultCounter(int totalCount, int controlCount)
         {
            TotalCount = totalCount;
            ControlCount = controlCount;
         }

         public int TotalCount { get; }
         public int ControlCount { get; }
      }
      private ResultCounter? _resultCounter;
   }
}

