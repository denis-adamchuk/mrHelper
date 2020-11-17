using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitLabSharp.Accessors;
using GitLabSharp.Entities;
using mrHelper.App.Helpers.GitLab;
using mrHelper.Common.Constants;
using mrHelper.Common.Tools;
using mrHelper.GitLabClient;

namespace mrHelper.App.Forms.Helpers
{
   internal static class DiscussionHelper
   {
      async internal static Task<bool> AddCommentAsync(GitLabClient.MergeRequestKey mrk, string title,
         IModificationListener modificationListener, User currentUser)
      {
         string caption = String.Format("Add comment to merge request \"{0}\"", title);
         DiscussionNoteEditPanel actions = new DiscussionNoteEditPanel();
         string uploadsPrefix = StringUtils.GetUploadsPrefix(mrk.ProjectKey);
         using (TextEditForm form = new TextEditForm(caption, "", true, true, actions, uploadsPrefix))
         {
            actions.SetTextbox(form.TextBox);
            if (form.ShowDialog() == DialogResult.OK)
            {
               if (form.Body.Length == 0)
               {
                  MessageBox.Show("Comment body cannot be empty", "Warning",
                     MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                  return false;
               }

               try
               {
                  GitLabInstance gitLabInstance = new GitLabInstance(mrk.ProjectKey.HostName, Program.Settings);
                  IDiscussionCreator creator = Shortcuts.GetDiscussionCreator(
                     gitLabInstance, modificationListener, mrk, currentUser);
                  string body = StringUtils.ConvertNewlineWindowsToUnix(form.Body);
                  await creator.CreateNoteAsync(new CreateNewNoteParameters(body));
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

      async internal static Task<Discussion> AddThreadAsync(GitLabClient.MergeRequestKey mrk, string title,
         IModificationListener modificationListener, User currentUser, DataCache dataCache)
      {
         string caption = String.Format("Create a new thread in merge request \"{0}\"", title);
         DiscussionNoteEditPanel actions = new DiscussionNoteEditPanel();
         string uploadsPrefix = StringUtils.GetUploadsPrefix(mrk.ProjectKey);
         using (TextEditForm form = new TextEditForm(caption, "", true, true, actions, uploadsPrefix))
         {
            actions.SetTextbox(form.TextBox);
            if (form.ShowDialog() == DialogResult.OK)
            {
               if (form.Body.Length == 0)
               {
                  MessageBox.Show("Discussion body cannot be empty", "Warning",
                     MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                  return null;
               }

               if (dataCache == null)
               {
                  Debug.Assert(false);
                  return null;
               }

               Discussion discussion = null;
               try
               {
                  GitLabInstance gitLabInstance = new GitLabInstance(mrk.ProjectKey.HostName, Program.Settings);
                  IDiscussionCreator creator = Shortcuts.GetDiscussionCreator(
                     gitLabInstance, modificationListener, mrk, currentUser);
                  string body = StringUtils.ConvertNewlineWindowsToUnix(form.Body);
                  discussion = await creator.CreateDiscussionAsync(new NewDiscussionParameters(body, null), false);
               }
               catch (DiscussionCreatorException)
               {
                  MessageBox.Show("Cannot create a discussion at GitLab. Check your connection and try again",
                     "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                  return null;
               }

               dataCache.DiscussionCache?.RequestUpdate(mrk, Constants.DiscussionCheckOnNewThreadInterval, null);
               return discussion;
            }

            return null;
         }
      }
   }
}

