using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.App.Forms;
using mrHelper.GitLabClient;

namespace mrHelper.App.Controls
{
   internal partial class ConnectionPage
   {
      private void textBoxDisplayFilter_TextChanged(object sender, EventArgs e)
      {
         onTextBoxDisplayFilterUpdate();
      }

      private void textBoxDisplayFilter_Leave(object sender, EventArgs e)
      {
         onTextBoxDisplayFilterUpdate();
      }

      private void checkBoxDisplayFilter_CheckedChanged(object sender, EventArgs e)
      {
         applyFilterChange((sender as CheckBox).Checked);
      }

      private void listViewMergeRequests_ContentChanged(object sender)
      {
         updateMergeRequestList(getListViewType(sender as Controls.MergeRequestListView));
      }

      private void listViewMergeRequests_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
      {
         onMergeRequestSelectionChanged(getListViewType(sender as Controls.MergeRequestListView));
      }

      private void revisionBrowser_SelectionChanged(object sender, EventArgs e)
      {
         CanDiffToolChanged?.Invoke(this);
      }

      private void splitContainer_SplitterMoved(object sender, SplitterEventArgs e)
      {
         SplitContainer splitter = sender as SplitContainer;
         if (isUserMovingSplitter(splitter))
         {
            onUserIsMovingSplitter(splitter, false);
         }
      }

      private void splitContainer_SplitterMoving(object sender, SplitterCancelEventArgs e)
      {
         onUserIsMovingSplitter(sender as SplitContainer, true);
      }

      private void linkLabelNewSearch_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         DataCache dataCache = getDataCache(EDataCacheType.Live);

         IEnumerable<Project> fullProjectList = dataCache?.ProjectCache?.GetProjects();
         if (fullProjectList == null)
         {
            fullProjectList = Array.Empty<Project>();
         }

         IEnumerable<string> projectNames = fullProjectList
            .Select(project => project.Path_With_Namespace);
         IEnumerable<User> fullUserList = dataCache?.UserCache?.GetUsers();
         if (fullUserList == null)
         {
            fullUserList = Array.Empty<User>();
         }

         if (_prevSearchQuery == null)
         {
            _prevSearchQuery = new EditSearchQueryFormState(getDefaultProjectName());
         }

         EditSearchQueryForm form = new EditSearchQueryForm(
            projectNames, fullUserList, CurrentUser, _prevSearchQuery);
         if (form.ShowDialog() == DialogResult.OK)
         {
            searchMergeRequests(new SearchQueryCollection(form.SearchQuery));
            _prevSearchQuery = form.State;
         }
      }

      private void tabControlMode_SelectedIndexChanged(object sender, EventArgs e)
      {
         onDataCacheSelectionChanged();
      }

      protected override void OnLoad(EventArgs e)
      {
         base.OnLoad(e);
         initializeWork();
      }

      protected override void OnFontChanged(EventArgs e)
      {
         base.OnFontChanged(e);

         processFontChange();
      }

      private void processFontChange()
      {
         if (!Created)
         {
            return;
         }

         Trace.TraceInformation(String.Format("[ConnectionPage] Font changed, new emSize = {0}", Font.Size));
         CommonControls.Tools.WinFormsHelpers.LogScaleDimensions(this);

         // see 9b65d7413c
         if (richTextBoxMergeRequestDescription.Location.X < 0
          || richTextBoxMergeRequestDescription.Location.Y < 0)
         {
            Trace.TraceWarning(
                  "Detected negative Location of Html Panel. "
                + "Location: {{{0}, {1}}}, Size: {{{2}, {3}}}. GroupBox Size: {{{4}, {5}}}",
               richTextBoxMergeRequestDescription.Location.X,
               richTextBoxMergeRequestDescription.Location.Y,
               richTextBoxMergeRequestDescription.Size.Width,
               richTextBoxMergeRequestDescription.Size.Height,
               groupBoxSelectedMR.Size.Width,
               groupBoxSelectedMR.Size.Height);
            Debug.Assert(false);
         }

         updateMergeRequestList(EDataCacheType.Live); // update row height of List View
         updateMergeRequestList(EDataCacheType.Search); // update row height of List View
         setFontSizeInMergeRequestDescriptionBox();
      }
   }
}
