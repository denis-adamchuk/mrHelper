using GitLabSharp.Entities;
using mrHelper.CommonControls.Controls;
using mrHelper.CommonControls.Tools;
using System;
using System.Windows.Forms;
using TheArtOfDev.HtmlRenderer.WinForms;

namespace mrHelper.App.Controls
{
   public interface ITextControl
   {
      string Text { get; }
      HighlightState HighlightState { get; }

      void HighlightFragment(int startPosition, int length);
      void ClearHighlight();
   }

   internal interface IHighlightListener
   {
      void OnHighlighted(Control control);
   }

   public class HighlightState
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
      public SearchableTextBox(IHighlightListener highlightListener)
      {
         _highlightListener = highlightListener;
      }

      public HighlightState HighlightState => new HighlightState(SelectionStart, SelectionLength);

      public void HighlightFragment(int startPosition, int length)
      {
         Select(startPosition, length);
         _highlightListener?.OnHighlighted(this);
      }

      public void ClearHighlight()
      {
         DeselectAll();
      }

      private readonly IHighlightListener _highlightListener;
   }

   internal class SearchableHtmlPanel : HtmlPanelEx, ITextControl
   {
      internal SearchableHtmlPanel(IHighlightListener highlightListener, RoundedPathCache pathCache)
         : base(pathCache, true, true)
      {
         /// Disable async image loading.
         /// Given feature prevents showing full-size images because their size are unknown
         /// at the moment of tooltip rendering.
         _htmlContainer.AvoidAsyncImagesLoading = true;

         _highlightListener = highlightListener;
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
         _highlightListener?.OnHighlighted(this);
      }

      public void ClearHighlight()
      {
         DiscussionNote note = getOriginalNote();
         if (note == null || HighlightState == null)
         {
            return;
         }

         // Unwrap a wrapped span (i.e. undo HighlightFragment).
         // Don't reset HighlightState to remember a place where highlighting was located in order to continue search.
         // Note: Parent can be null if a note was deleted.
         (Parent as DiscussionBox)?.setDiscussionNoteText(this, note);
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

      private readonly IHighlightListener _highlightListener;
   }
}

