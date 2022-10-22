using System;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.App.Helpers;

namespace mrHelper.App.Forms
{
   internal abstract partial class ReplyOnNoteBaseForm : TextEditBaseForm
   {
      internal ReplyOnNoteBaseForm(string uploadsPrefix, IEnumerable<User> fullUserList, AvatarImageCache avatarImageCache)
         : base("Reply on a Discussion", "", true, true, uploadsPrefix, fullUserList, avatarImageCache)
      {
      }
   }

   internal partial class ReplyOnDiscussionNoteForm : ReplyOnNoteBaseForm
   {
      internal ReplyOnDiscussionNoteForm(string resolveText, bool proposeUserToToggleResolveOnReply, string uploadsPrefix,
         IEnumerable<User> fullUserList, AvatarImageCache avatarImageCache)
         : base(uploadsPrefix, fullUserList, avatarImageCache)
      {
         Controls.NoteEditPanel p = new Controls.NoteEditPanel(resolveText, proposeUserToToggleResolveOnReply, onInsertCode);
         _isResolveChecked = () => p.IsResolveActionChecked;
         setExtraActionsControl(p);
      }

      internal bool IsResolveActionChecked => _isResolveChecked();
      private readonly Func<bool> _isResolveChecked;
   }

   internal partial class ReplyOnRelatedNoteForm : ReplyOnNoteBaseForm
   {
      internal ReplyOnRelatedNoteForm(string uploadsPrefix, IEnumerable<User> fullUserList, AvatarImageCache avatarImageCache)
         : base(uploadsPrefix, fullUserList, avatarImageCache)
      {
         Controls.ReplyOnRelatedNotePanel p = new Controls.ReplyOnRelatedNotePanel(true, onInsertCode);
         _isCloseDialogChecked = () => p.IsCloseDialogActionChecked;
         setExtraActionsControl(p);
      }

      internal bool IsCloseDialogActionChecked => _isCloseDialogChecked();
      private readonly Func<bool> _isCloseDialogChecked;
   }

   internal partial class SimpleTextEditForm : TextEditBaseForm
   {
      internal SimpleTextEditForm(string caption, string initialText, bool multiline, string uploadsPrefix,
         IEnumerable<User> fullUserList, AvatarImageCache avatarImageCache)
         : base(caption, initialText, true, multiline, uploadsPrefix, fullUserList, avatarImageCache)
      {
         setExtraActionsControl(new Controls.NoteEditPanel(onInsertCode));
      }
   }

   internal partial class EditNoteForm : TextEditBaseForm
   {
      internal EditNoteForm(string initialText, string uploadsPrefix, IEnumerable<User> fullUserList, AvatarImageCache avatarImageCache)
         : base("Edit Discussion Note", initialText, true, true, uploadsPrefix, fullUserList, avatarImageCache)
      {
         setExtraActionsControl(new Controls.NoteEditPanel(onInsertCode));
      }
   }

   internal partial class ViewNoteForm : TextEditBaseForm
   {
      internal ViewNoteForm(string initialText, string uploadsPrefix)
         : base("View Discussion Note", initialText, false, true, uploadsPrefix, null, null)
      {
      }
   }
}

