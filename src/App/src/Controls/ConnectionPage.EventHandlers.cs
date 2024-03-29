﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.App.Forms;
using mrHelper.GitLabClient;
using mrHelper.CommonControls.Tools;

namespace mrHelper.App.Controls
{
   internal partial class ConnectionPage
   {
      private void textBoxDisplayFilter_TextChanged(object sender, EventArgs e)
      {
         onTextBoxDisplayFilterUpdate(EDataCacheType.Live, textBoxDisplayFilter.GetFullText());
      }

      private void textBoxDisplayFilterRecent_TextChanged(object sender, EventArgs e)
      {
         onTextBoxDisplayFilterUpdate(EDataCacheType.Recent, textBoxDisplayFilterRecent.GetFullText());
      }

      private void comboBoxFilter_SelectionChangeCommitted(object sender, EventArgs e)
      {
         onCheckBoxDisplayFilterUpdate(EDataCacheType.Live, comboBoxFilter.GetSelected());
      }

      private void comboBoxFilterRecent_SelectionChangeCommitted(object sender, EventArgs e)
      {
         onCheckBoxDisplayFilterUpdate(EDataCacheType.Recent, comboBoxFilterRecent.GetSelected());
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
         if (_prevSearchQuery == null)
         {
            _prevSearchQuery = new EditSearchQueryFormState(getDefaultProjectName());
         }

         IEnumerable<User> fullUserList = getUsers();
         IEnumerable<string> projectNames = getProjects().Select(project => project.Path_With_Namespace);
         using (EditSearchQueryForm form = new EditSearchQueryForm(
            projectNames, fullUserList, CurrentUser, _prevSearchQuery))
         {
            if (WinFormsHelpers.ShowDialogOnControl(form, WinFormsHelpers.FindMainForm()) == DialogResult.OK)
            {
               searchMergeRequests(new SearchQueryCollection(form.SearchQuery));
               _prevSearchQuery = form.State;
            }
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

         updateMergeRequestList(EDataCacheType.Live); // update row height of List View
         updateMergeRequestList(EDataCacheType.Search); // update row height of List View
      }
   }
}
