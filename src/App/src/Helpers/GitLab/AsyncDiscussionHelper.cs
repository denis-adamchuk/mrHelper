using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitLabSharp.Accessors;
using GitLabSharp.Entities;
using mrHelper.App.Forms;
using mrHelper.App.Controls;
using mrHelper.Common.Tools;
using mrHelper.GitLabClient;

namespace mrHelper.App.Helpers
{
   internal class AsyncDiscussionHelper
   {
      internal AsyncDiscussionHelper(MergeRequestKey mrk, string title,
         User currentUser, GitLab.Shortcuts shortcuts)
      {
         _creator = shortcuts.GetDiscussionCreator(mrk, currentUser);
         _uploadsPrefix = StringUtils.GetUploadsPrefix(mrk.ProjectKey);
         _title = title;
      }

      internal Task<bool> AddCommentAsync()
      {
         string caption = String.Format("Add comment to merge request \"{0}\"", _title);
         return createDiscussion(caption, (body) =>
            _creator.CreateNoteAsync(new CreateNewNoteParameters(body)));
      }

      internal Task<bool> AddThreadAsync()
      {
         string caption = String.Format("Create a new thread in merge request \"{0}\"", _title);
         return createDiscussion(caption, (body) =>
            _creator.CreateDiscussionAsync(new NewDiscussionParameters(body, null), false));
      }

      async private Task<bool> createDiscussion(string title, Func<string, Task> funcCreator)
      {
         NoteEditPanel actions = new NoteEditPanel();
         using (TextEditForm form = new TextEditForm(title, "", true, true, actions, _uploadsPrefix))
         {
            actions.SetTextbox(form.TextBox);
            if (form.ShowDialog() == DialogResult.OK)
            {
               if (form.Body.Length == 0)
               {
                  MessageBox.Show("Body cannot be empty", "Warning",
                     MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                  return false;
               }

               try
               {
                  string body = StringUtils.ConvertNewlineWindowsToUnix(form.Body);
                  await funcCreator(body);
               }
               catch (DiscussionCreatorException)
               {
                  MessageBox.Show("Cannot create a discussion at GitLab. Check your connection and try again",
                     "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                  return false;
               }

               return true;
            }

            return false;
         }
      }

      private readonly IDiscussionCreator _creator;
      private readonly string _uploadsPrefix;
      private readonly string _title;
   }
}

