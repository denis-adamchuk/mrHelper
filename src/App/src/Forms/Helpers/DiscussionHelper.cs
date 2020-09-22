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
         string uploadsPrefix = String.Format("{0}/{1}",
            StringUtils.GetHostWithPrefix(mrk.ProjectKey.HostName), mrk.ProjectKey.ProjectName);

         string caption = String.Format("Add comment to merge request \"{0}\"", title);
         DiscussionNoteEditPanel actions = new DiscussionNoteEditPanel();
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
                  await creator.CreateNoteAsync(new CreateNewNoteParameters(form.Body));
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

      async internal static Task<bool> AddThreadAsync(GitLabClient.MergeRequestKey mrk, string title,
         IModificationListener modificationListener, User currentUser, DataCache dataCache)
      {
         string uploadsPrefix = String.Format("{0}/{1}",
            StringUtils.GetHostWithPrefix(mrk.ProjectKey.HostName), mrk.ProjectKey.ProjectName);

         string caption = String.Format("Create a new thread in merge request \"{0}\"", title);
         DiscussionNoteEditPanel actions = new DiscussionNoteEditPanel();
         using (TextEditForm form = new TextEditForm(caption, "", true, true, actions, uploadsPrefix))
         {
            actions.SetTextbox(form.TextBox);
            if (form.ShowDialog() == DialogResult.OK)
            {
               if (form.Body.Length == 0)
               {
                  MessageBox.Show("Discussion body cannot be empty", "Warning",
                     MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                  return false;
               }

               if (dataCache == null)
               {
                  Debug.Assert(false);
                  return false;
               }

               try
               {
                  GitLabInstance gitLabInstance = new GitLabInstance(mrk.ProjectKey.HostName, Program.Settings);
                  IDiscussionCreator creator = Shortcuts.GetDiscussionCreator(
                     gitLabInstance, modificationListener, mrk, currentUser);
                  await creator.CreateDiscussionAsync(new NewDiscussionParameters(form.Body, null), false);
               }
               catch (DiscussionCreatorException)
               {
                  MessageBox.Show("Cannot create a discussion at GitLab. Check your connection and try again",
                     "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                  return false;
               }

               dataCache.DiscussionCache?.RequestUpdate(mrk, Constants.DiscussionCheckOnNewThreadInterval, null);
               return true;
            }

            return false;
         }
      }
   }
}

