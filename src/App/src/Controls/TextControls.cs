using GitLabSharp.Entities;
using mrHelper.CommonControls.Controls;
using System;
using System.Windows.Forms;
using TheArtOfDev.HtmlRenderer.WinForms;

namespace mrHelper.App.Controls
{
   internal interface ITextControl
   {
      string Text { get; }
      HighlightState HighlightState { get; }

      void HighlightFragment(int startPosition, int length);
      void ClearHighlight();
   }

   internal class HighlightState
   {
      public HighlightState(int highlightStart, int highlightLength)
      {
         HighlightStart = highlightStart;
         HighlightLength = highlightLength;
      }

      public readonly int HighlightStart;
      public readonly int HighlightLength;
   }

   internal class SearchableTextBox : TextBoxNoWheel, ITextControl
   {
      public HighlightState HighlightState => new HighlightState(SelectionStart, SelectionLength);

      public void HighlightFragment(int startPosition, int length)
      {
         Select(startPosition, length);
      }

      public void ClearHighlight()
      {
         DeselectAll();
      }
   }

   internal class HtmlPanelWithGoodImages : HtmlPanel, ITextControl
   {
      internal HtmlPanelWithGoodImages()
         : base()
      {
         /// Disable async image loading.
         /// Given feature prevents showing full-size images because their size are unknown
         /// at the moment of tooltip rendering.
         _htmlContainer.AvoidAsyncImagesLoading = true;
      }

      string ITextControl.Text => Tag == null ? String.Empty : ((DiscussionNote)Tag).Body;

      public HighlightState HighlightState { get; private set; }

      public void HighlightFragment(int startPosition, int length)
      {
         DiscussionNote note = (DiscussionNote)Tag;
         if (note == null)
         {
            return;
         }

         string discussionText = note.Body;
         string prefix = "<span class=\"highlight\">";
         string suffix = "</span>";
         string newText = discussionText
            .Insert(startPosition, prefix)
            .Insert(startPosition + length + prefix.Length, suffix);

         DiscussionNote newNote = new DiscussionNote(note.Id, newText, note.Created_At, note.Updated_At,
            note.Author, note.Type, note.System, note.Resolvable, note.Resolved, note.Position);
         (Parent as DiscussionBox).setDiscussionNoteHtmlText(this, newNote);
         HighlightState = new HighlightState(startPosition, length);
      }

      public void ClearHighlight()
      {
         DiscussionNote note = (DiscussionNote)Tag;
         if (note == null)
         {
            return;
         }

         (Parent as DiscussionBox).setDiscussionNoteHtmlText(this, note);
         HighlightState = null;
      }

      protected override void OnMouseDown(MouseEventArgs e)
      {
         base.OnMouseDown(e);
         ClearHighlight();
      }

      protected override void OnLostFocus(EventArgs e)
      {
         base.OnLostFocus(e);
         ClearHighlight();
      }
   }
}

