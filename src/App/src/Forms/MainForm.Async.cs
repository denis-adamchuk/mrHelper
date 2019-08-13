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
using mrHelper.CustomActions;
using mrHelper.Common.Interfaces;
using mrHelper.Core;
using mrHelper.Client;
using mrHelper.Forms;

namespace mrHelper.App.Forms
{
   internal partial class mrHelperForm
   {
      async private Task showDiscussionsFormAsync()
      {
         _gitClient = null;
         prepareToAsyncGitOperation();
         try
         {
            _gitClient = await _gitClientInitializer.InitAsync(_settings.LocalGitFolder, GetCurrentHostName(),
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
            form = new DiscussionsForm(_workflow.State.MergeRequestDescriptor, gitClient,
               int.Parse(comboBoxDCDepth.Text), _colorScheme, discussions, _discussionManager,
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
         List<Discussion> discussions;
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

      async private void onLaunchDiffTool()
      {
         _diffToolArgs = null;
         await updateInterprocessSnapshot(); // to purge serialized snapshot

         _gitClient = null;
         prepareToAsyncGitOperation();
         try
         {
            _gitClient = await _gitClientInitializer.InitAsync(_settings.LocalGitFolder, GetCurrentHostName(),
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
            return;
         }
         finally
         {
            fixAfterAsyncGitOperation();
         }

         string leftSHA = getGitTag(true /* left */);
         string rightSHA = getGitTag(false /* right */);

         buttonDiffTool.Enabled = false;
         buttonDiscussions.Enabled = false;

         try
         {
            await gitClient.DiffToolAsync(name, leftCommit, rightCommit);
         }
         catch (GitOperationException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot launch diff tool");
            MessageBox.Show("Cannot launch diff tool", "Error",
                  MessageBoxButtons.OK, MessageBoxIcon.Error);
         }

         _diffToolArgs = new DiffToolArguments
         {
            LeftSHA = leftSHA,
            RightSHA = rightSHA
         };

         await updateInterprocessSnapshot();
      }

      async private Task updateInterprocessSnapshot()
      {
         // first of all, delete old snapshot
         SnapshotSerializer serializer = new SnapshotSerializer();
         serializer.PurgeSerialized();

         bool allowReportingIssues = !checkBoxRequireTimer.Checked || _timeTrackingTimer.Enabled;
         if (!allowReportingIssues || !_diffToolArgs.HasValue
          || GetCurrentHostName() == null || GetCurrentProjectName() == null)
         {
            return;
         }

         MergeRequest? mergeRequest = await loadMergeRequestAsync();
         if (!mergeRequest.HasValue)
         {
            return;
         }

         Snapshot snapshot;
         snapshot.AccessToken = GetCurrentAccessToken();
         snapshot.Refs.LeftSHA = _diffToolArgs.Value.LeftSHA;     // Base commit SHA in the source branch
         snapshot.Refs.RightSHA = _diffToolArgs.Value.RightSHA;   // SHA referencing HEAD of this merge request
         snapshot.Host = GetCurrentHostName();
         snapshot.MergeRequestIId = mergeRequest.Value.IId;
         snapshot.Project = GetCurrentProjectName();
         snapshot.TempFolder = textBoxLocalGitFolder.Text;

         serializer.SerializeToDisk(snapshot);
      }

      async private Task onStartTimer()
      {
         // 1. Update button text
         buttonToggleTimer.Text = buttonStartTimerTrackingText;

         // 2. Set default text to tracked time label
         labelSpentTime.Text = labelSpentTimeDefaultText;

         // 3. Start timer
         _timeTrackingTimer.Start();

         // 4. Reset and start stopwatch
         _timeTracker = _timeTrackingManager.GetTracker(_workflow.State.MergeRequestDescriptor);
         _timeTracker.Start();

         // 5. Update information available to other instances
         await updateInterprocessSnapshot();
      }

      async private Task onStopTimer()
      {
         // 1. Stop stopwatch and send tracked time
         string span = _timeTracker.Elapsed;
         if (sendTrackedTime && span.Seconds > 1)
         {
            labelWorkflowStatus.Text = "Sending tracked time...";
            string duration = span.ToString("hh") + "h " + span.ToString("mm") + "m " + span.ToString("ss") + "s";
            string status = String.Format("Tracked time {0} sent successfully", duration);
            try
            {
               _timeTracker.Stop();
            }
            catch (TimeTrackerException)
            {
               status = "Error occurred. Tracked time is not sent!";
            }
            labelWorkflowStatus.Text = status;
         }
         _timeTracker = null;

         // 2. Stop timer
         _timeTrackingTimer.Stop();

         // 3. Update information available to other instances
         await updateInterprocessSnapshot();

         // 4. Set default text to tracked time label
         labelSpentTime.Text = labelSpentTimeDefaultText;

         // 5. Update button text
         buttonToggleTimer.Text = buttonStartTimerDefaultText;
      }
   }
}

