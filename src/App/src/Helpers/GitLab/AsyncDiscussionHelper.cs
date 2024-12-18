﻿using System;
using System.Linq;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp.Accessors;
using GitLabSharp.Entities;
using mrHelper.App.Forms;
using mrHelper.Common.Tools;
using mrHelper.GitLabClient;
using mrHelper.CommonControls.Tools;

namespace mrHelper.App.Helpers
{
   internal class AsyncDiscussionHelper
   {
      internal AsyncDiscussionHelper(MergeRequestKey mrk, string title,
         User currentUser, GitLab.Shortcuts shortcuts, IEnumerable<User> fullUserList,
         IEnumerable<Project> fullProjectList, AvatarImageCache avatarImageCache)
      {
         _creator = shortcuts.GetDiscussionCreator(mrk, currentUser);
         Project project = fullProjectList.FirstOrDefault(p => p.Path_With_Namespace == mrk.ProjectKey.ProjectName);
         _uploadsPrefix = StringUtils.GetUploadsPrefix(mrk.ProjectKey.HostName, project?.Id ?? 0);
         _title = title;
         _fullUserList = fullUserList;
         _avatarImageCache = avatarImageCache;
      }

      internal Task<bool> AddCommentAsync(Form parentForm)
      {
         string caption = String.Format("Add comment to merge request \"{0}\"", _title);
         return createDiscussion(parentForm, caption, (body) =>
            _creator.CreateNoteAsync(new CreateNewNoteParameters(body)));
      }

      internal Task<bool> AddThreadAsync(Form parentForm)
      {
         string caption = String.Format("Create a new thread in merge request \"{0}\"", _title);
         return createDiscussion(parentForm, caption, (body) =>
            _creator.CreateDiscussionAsync(new NewDiscussionParameters(body, null), false));
      }

      async private Task<bool> createDiscussion(Form parentForm, string title, Func<string, Task> funcCreator)
      {
         using (TextEditBaseForm form = new SimpleTextEditForm(title, String.Empty, true, _uploadsPrefix,
            _fullUserList, _avatarImageCache))
         {
            if (WinFormsHelpers.ShowDialogOnControl(form, parentForm) == DialogResult.OK)
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
      private readonly IEnumerable<User> _fullUserList;
      private readonly AvatarImageCache _avatarImageCache;
   }
}

