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

   internal class SearchableTextBox : TextBoxEx, ITextControl
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

      string ITextControl.Text => removeCodeBlocks(getOriginalNote()).Body;

      public HighlightState HighlightState { get; private set; }

      public void HighlightFragment(int startPosition, int length)
      {
         DiscussionNote note = removeCodeBlocks(getOriginalNote());
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

         (Parent as DiscussionBox).setDiscussionNoteHtmlText(this, cloneNoteWithNewText(getOriginalNote(), newText));
         HighlightState = new HighlightState(startPosition, length);
      }

      public void ClearHighlight()
      {
         DiscussionNote note = getOriginalNote();
         if (note == null)
         {
            return;
         }

         (Parent as DiscussionBox).setDiscussionNoteHtmlText(this, note);
         HighlightState = null;
      }

      protected override void OnMouseDown(MouseEventArgs e)
      {
         ClearHighlight();
         base.OnMouseDown(e);
      }

      protected override void OnLostFocus(EventArgs e)
      {
         ClearHighlight();
         base.OnLostFocus(e);
      }

      private DiscussionNote getOriginalNote()
      {
         return (DiscussionNote)Tag;
      }

      private DiscussionNote removeCodeBlocks(DiscussionNote note)
      {
         DiscussionNote originalNote = getOriginalNote();
         string originalBody = originalNote.Body;
         string newBody = originalBody.Replace("`", "").Replace("~", "");
         return cloneNoteWithNewText(originalNote, newBody);
      }

      private DiscussionNote cloneNoteWithNewText(DiscussionNote note, string text)
      {
         return new DiscussionNote(note.Id, text, note.Created_At, note.Updated_At,
            note.Author, note.Type, note.System, note.Resolvable, note.Resolved, note.Position);
      }
   }
}

