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

   internal class SearchableHtmlPanel : HtmlPanel, ITextControl
   {
      internal SearchableHtmlPanel()
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

         string span = wrapTextFragmentInSpan(startPosition, length, note);
         DiscussionNote updatedNote = cloneNoteWithNewText(getOriginalNote(), span);
         (Parent as DiscussionBox).setDiscussionNoteText(this, updatedNote);
         HighlightState = new HighlightState(startPosition, length);
      }

      public void ClearHighlight()
      {
         DiscussionNote note = getOriginalNote();
         if (note == null)
         {
            return;
         }

         (Parent as DiscussionBox).setDiscussionNoteText(this, note);
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

      private static string wrapTextFragmentInSpan(int startPosition, int length, DiscussionNote note)
      {
         string discussionText = note.Body;
         string prefix = "<span class=\"highlight\">";
         string suffix = "</span>";
         string newText = discussionText
            .Insert(startPosition, prefix)
            .Insert(startPosition + length + prefix.Length, suffix);
         return newText;
      }

      private DiscussionNote removeCodeBlocks(DiscussionNote note)
      {
         if (note == null)
         {
            return null;
         }

         string oldBody = note.Body;
         string newBody = oldBody.Replace("`", "").Replace("~", "");
         return cloneNoteWithNewText(note, newBody);
      }

      private DiscussionNote cloneNoteWithNewText(DiscussionNote note, string text)
      {
         return new DiscussionNote(note.Id, text, note.Created_At, note.Updated_At,
            note.Author, note.Type, note.System, note.Resolvable, note.Resolved, note.Position);
      }
   }
}

