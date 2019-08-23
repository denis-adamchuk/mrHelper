using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.App.Helpers;
using mrHelper.CustomActions;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;
using mrHelper.Core;
using mrHelper.Core.Interprocess;
using mrHelper.Client.Tools;
using mrHelper.Client.Workflow;
using mrHelper.Client.Discussions;
using mrHelper.Client.Git;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
      async private Task showDiscussionsFormAsync()
      {
         GitClient client = getGitClient();
         if (client != null)
         {
            preGitClientInitialize();
            try
            {
               await _gitClientUpdater.UpdateAsync(client);
            }
            catch (Exception ex)
            {
               if (ex is RepeatOperationException)
               {
                  return;
               }
               else if (ex is CancelledByUserException)
               {
                  if (MessageBox.Show("Without up-to-date git repository, some context code snippets might be missing. "
                     + "Do you want to continue?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) ==
                        DialogResult.No)
                  {
                     return;
                  }
                  else
                  {
                     client = null;
                  }
               }
               else
               {
                  Debug.Assert(ex is ArgumentException || ex is GitOperationException);
                  ExceptionHandlers.Handle(ex, "Cannot initialize/update git repository");
                  MessageBox.Show("Cannot initialize git repository",
                     "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                  return;
               }
            }
            finally
            {
               postGitClientInitialize();
            }
         }
         else
         {
            if (MessageBox.Show("Without git repository, context code snippets will be missing. "
               + "Do you want to continue?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) ==
                  DialogResult.No)
            {
               return;
            }
            else
            {
               client = null;
            }
         }

         User? currentUser = await loadCurrentUserAsync();
         List<Discussion> discussions = await loadDiscussionsAsync();
         if (!currentUser.HasValue || discussions == null)
         {
            return;
         }

         labelWorkflowStatus.Text = "Rendering Discussions Form...";
         labelWorkflowStatus.Update();

         DiscussionsForm form;
         try
         {
            form = new DiscussionsForm(_workflow.State.MergeRequestDescriptor, _workflow.State.MergeRequest.Author,
               client, int.Parse(comboBoxDCDepth.Text), _colorScheme, discussions, _discussionManager,
               currentUser.Value);
         }
         catch (NoDiscussionsToShow)
         {
            MessageBox.Show("No discussions to show.", "Information",
               MessageBoxButtons.OK, MessageBoxIcon.Information);
            labelWorkflowStatus.Text = "No discussions to show";
            return;
         }
         catch (ArgumentException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot show Discussions form");
            MessageBox.Show("Cannot show Discussions form", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            labelWorkflowStatus.Text = "Cannot show Discussions";
            return;
         }

         labelWorkflowStatus.Text = "Discussions opened";

         form.Show();
      }

      async private void onLaunchDiffTool()
      {
         GitClient client = getGitClient();
         if (client != null)
         {
            preGitClientInitialize();
            try
            {
               await _gitClientUpdater.UpdateAsync(client);
            }
            catch (Exception ex)
            {
               if (ex is CancelledByUserException)
               {
                  // User declined to create a repository
                  MessageBox.Show("Cannot launch a diff tool without up-to-date git repository", "Warning",
                     MessageBoxButtons.OK, MessageBoxIcon.Warning);
               }
               else if (!(ex is RepeatOperationException))
               {
                  Debug.Assert(ex is ArgumentException || ex is GitOperationException);
                  ExceptionHandlers.Handle(ex, "Cannot initialize/update git repository");
                  MessageBox.Show("Cannot initialize git repository",
                     "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
               }
               return;
            }
            finally
            {
               postGitClientInitialize();
            }
         }
         else
         {
            MessageBox.Show("Cannot launch a diff tool without up-to-date git repository", "Warning",
               MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
         }

         if (comboBoxLeftCommit.SelectedItem == null || comboBoxRightCommit.SelectedItem == null)
         {
            // State changed during async git client update
            return;
         }

         string leftSHA = getGitTag(true /* left */);
         string rightSHA = getGitTag(false /* right */);

         labelWorkflowStatus.Text = "Launching diff tool...";

         int pid;
         try
         {
            pid = client.DiffTool(mrHelper.DiffTool.DiffToolIntegration.GitDiffToolName,
               leftSHA, rightSHA);
         }
         catch (GitOperationException ex)
         {
            string message = "Could not launch diff tool";
            ExceptionHandlers.Handle(ex, message);
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            labelWorkflowStatus.Text = message;
            return;
         }

         labelWorkflowStatus.Text = "Diff tool launched";

         saveInterprocessSnapshot(pid, leftSHA, rightSHA);
      }

      private void saveInterprocessSnapshot(int pid, string leftSHA, string rightSHA)
      {
         Snapshot snapshot;
         snapshot.AccessToken = GetCurrentAccessToken();
         snapshot.Refs.LeftSHA = leftSHA;     // Base commit SHA in the source branch
         snapshot.Refs.RightSHA = rightSHA;   // SHA referencing HEAD of this merge request
         snapshot.Host = GetCurrentHostName();
         snapshot.MergeRequestIId = GetCurrentMergeRequestIId();
         snapshot.Project = GetCurrentProjectName();
         snapshot.TempFolder = textBoxLocalGitFolder.Text;

         SnapshotSerializer serializer = new SnapshotSerializer();
         serializer.SerializeToDisk(snapshot, pid);
      }

      async private Task<User?> loadCurrentUserAsync()
      {
         labelWorkflowStatus.Text = "Loading current user...";
         User? currentUser;
         try
         {
            currentUser = await _workflow.GetCurrentUser();
         }
         catch (WorkflowException)
         {
            string message = "Cannot load current user details from GitLab";
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            labelWorkflowStatus.Text = message;
            return null;
         }
         labelWorkflowStatus.Text = "Loaded current user details";
         return currentUser;
      }

      async private Task<List<Discussion>> loadDiscussionsAsync()
      {
         labelWorkflowStatus.Text = "Loading discussions...";
         List<Discussion> discussions;
         try
         {
            discussions = await _discussionManager.GetDiscussionsAsync(_workflow.State.MergeRequestDescriptor);
         }
         catch (DiscussionManagerException)
         {
            string message = "Cannot load discussions from GitLab";
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            labelWorkflowStatus.Text = message;
            return null;
         }
         labelWorkflowStatus.Text = "Loaded discussions";
         return discussions;
      }
   }
}

