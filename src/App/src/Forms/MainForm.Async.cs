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

namespace mrHelper.App.Forms
{
   internal partial class mrHelperForm
   {
      async private Task showDiscussionsFormAsync()
      {
         string path = Path.Combine(_settings.LocalGitFolder, GetCurrentProjectName().Split('/')[1]);
         createGitClient(_settings.LocalGitFolder, path);
         if (_gitClient == null)
         {
            return;
         }

         prepareToAsyncGitOperation();
         try
         {
            await _gitClientInitializer.InitAsync(_gitClient, path, GetCurrentHostName(),
               GetCurrentProjectName(), _commitChecker);
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
            fixAfterAsyncGitOperation();
         }

         User? currentUser = await loadCurrentUserAsync();
         List<Discussion> discussions = await loadDiscussionsAsync();
         if (!currentUser.HasValue || discussions == null)
         {
            return;
         }

         labelWorkflowStatus.Text = "Rendering Discussions Form...";
         labelWorkflowStatus.Update();

         DiscussionsForm form = null;
         try
         {
            form = new DiscussionsForm(_workflow.State.MergeRequestDescriptor, _workflow.State.MergeRequest.Author,
               _gitClient, int.Parse(comboBoxDCDepth.Text), _colorScheme, discussions, _discussionManager,
               currentUser.Value);
         }
         catch (NoDiscussionsToShow)
         {
            MessageBox.Show("No discussions to show.", "Information",
               MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
         }
         catch (ArgumentException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot show Discussions form");
            MessageBox.Show("Cannot show Discussions form", "Error",
                  MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
         }
         finally
         {
            labelWorkflowStatus.Text = String.Empty;
         }

         form.Show();
      }

      async private void onLaunchDiffTool()
      {
         _diffToolArgs = null;
         updateInterprocessSnapshot(); // to purge serialized snapshot

         string path = Path.Combine(_settings.LocalGitFolder, GetCurrentProjectName().Split('/')[1]);
         createGitClient(_settings.LocalGitFolder, path);
         if (_gitClient == null)
         {
            return;
         }

         prepareToAsyncGitOperation();
         try
         {
            await _gitClientInitializer.InitAsync(_gitClient, path, GetCurrentHostName(),
               GetCurrentProjectName(), _commitChecker);
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

            fixAfterAsyncGitOperation();
            return;
         }

         string leftSHA = getGitTag(true /* left */);
         string rightSHA = getGitTag(false /* right */);

         try
         {
            await _gitClient.DiffToolAsync(mrHelper.DiffTool.DiffToolIntegration.GitDiffToolName,
               leftSHA, rightSHA);
         }
         catch (GitOperationException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot launch diff tool");
            MessageBox.Show("Cannot launch diff tool", "Error",
                  MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
         finally
         {
            fixAfterAsyncGitOperation();
         }

         _diffToolArgs = new DiffToolArguments
         {
            LeftSHA = leftSHA,
            RightSHA = rightSHA
         };

         updateInterprocessSnapshot();
      }

      private void updateInterprocessSnapshot()
      {
         // first of all, delete old snapshot
         SnapshotSerializer serializer = new SnapshotSerializer();
         serializer.PurgeSerialized();

         bool allowReportingIssues = !checkBoxRequireTimer.Checked || _timeTrackingTimer.Enabled;
         if (!allowReportingIssues || !_diffToolArgs.HasValue)
         {
            return;
         }

         Snapshot snapshot;
         snapshot.AccessToken = GetCurrentAccessToken();
         snapshot.Refs.LeftSHA = _diffToolArgs.Value.LeftSHA;     // Base commit SHA in the source branch
         snapshot.Refs.RightSHA = _diffToolArgs.Value.RightSHA;   // SHA referencing HEAD of this merge request
         snapshot.Host = GetCurrentHostName();
         snapshot.MergeRequestIId = GetCurrentMergeRequestIId();
         snapshot.Project = GetCurrentProjectName();
         snapshot.TempFolder = textBoxLocalGitFolder.Text;

         serializer.SerializeToDisk(snapshot);
      }

      async private Task<User?> loadCurrentUserAsync()
      {
         labelWorkflowStatus.Text = "Loading current user...";
         User? currentUser = null;
         try
         {
            currentUser = await _workflow.GetCurrentUser();
         }
         catch (WorkflowException)
         {
            MessageBox.Show("Cannot load current user", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
         labelWorkflowStatus.Text = String.Empty;
         return currentUser;
      }

      async private Task<List<Discussion>> loadDiscussionsAsync()
      {
         labelWorkflowStatus.Text = "Loading discussions...";
         List<Discussion> discussions = null;
         try
         {
            discussions = await _discussionManager.GetDiscussionsAsync(_workflow.State.MergeRequestDescriptor);
         }
         catch (DiscussionManagerException)
         {
            MessageBox.Show("Cannot load discussions", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
         labelWorkflowStatus.Text = String.Empty;
         return discussions;
      }
   }
}

