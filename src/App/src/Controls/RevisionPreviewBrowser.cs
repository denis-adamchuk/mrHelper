using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Aga.Controls.Tree;
using Aga.Controls.Tree.NodeControls;
using mrHelper.App.Helpers;

namespace mrHelper.App.Controls
{
   internal partial class RevisionPreviewBrowser : UserControl
   {
      readonly bool _initializing;
      public RevisionPreviewBrowser()
      {
         _initializing = true;
         InitializeComponent();
         _initializing = false;

         _treeView.Model = new RevisionPreviewBrowserModel();
         _treeView.RowDraw += onTreeViewDrawRow;
         _treeView.DrawGridLine += onTreeViewDrawGridLine;
         _treeView.DrawControl += onTreeViewDrawControl;
         _treeView.SetToolTip(toolTip);
         RevisionBrowserDrawingHelper.ApplyFont(_treeView, this.Font);
      }

      internal void SetData(RevisionPreviewBrowserModelData data)
      {
         getModel().Data = data;

         _treeView.ExpandAll();

         if (_treeView.SelectedNode != null)
         {
            _treeView.EnsureVisible(_treeView.SelectedNode);
         }
      }

      internal void ClearData()
      {
         SetData(new RevisionPreviewBrowserModelData());
      }

      protected override void OnLoad(EventArgs e)
      {
         base.OnLoad(e);
         if (Program.Settings != null)
         {
            loadColumnWidths(Program.Settings.RevisionPreviewBrowserColumnWidths);
         }
      }

      protected override void OnVisibleChanged(EventArgs e)
      {
         base.OnVisibleChanged(e);
         if (Visible && Program.Settings != null)
         {
            loadColumnWidths(Program.Settings.RevisionPreviewBrowserColumnWidths);
         }
      }

      protected override void OnFontChanged(EventArgs eventArgs)
      {
         base.OnFontChanged(eventArgs);
         RevisionBrowserDrawingHelper.ApplyFont(_treeView, this.Font);
      }

      private void onTreeViewDrawRow(object sender, TreeViewRowDrawEventArgs args)
      {
         RevisionBrowserDrawingHelper.DrawRow(_treeView, args);
      }

      private void onTreeViewDrawGridLine(object sender, TreeViewGridLineDrawEventArgs args)
      {
         RevisionBrowserDrawingHelper.DrawGridLine(args);
      }

      private void onDrawColHeaderBg(object sender, DrawColHeaderBgEventArgs args)
      {
         RevisionBrowserDrawingHelper.DrawColumnHeaderBackground(args);
      }

      private void onDrawColHeaderText(object sender, DrawColHeaderTextEventArgs args)
      {
         RevisionBrowserDrawingHelper.DrawColumnHeaderText(_treeView, args);
      }

      private void onTreeViewDrawControl(object sender, DrawEventArgs args)
      {
         if (args is DrawTextEventArgs)
         {
            RevisionBrowserDrawingHelper.DrawNode(args as DrawTextEventArgs);
         }
      }

      private void onTreeViewColumnWidthChanged(object sender, TreeColumnEventArgs e)
      {
         if (!_loadingColumnWidth && !_initializing)
         {
            saveColumnWidths(x => Program.Settings.RevisionPreviewBrowserColumnWidths = x);
         }
      }

      private RevisionPreviewBrowserModel getModel()
      {
         return _treeView.Model as RevisionPreviewBrowserModel;
      }

      private void saveColumnWidths(Action<Dictionary<string, int>> saveProperty)
      {
         Dictionary<string, int> columnWidths = new Dictionary<string, int>();
         foreach (TreeColumn column in _treeView.Columns)
         {
            columnWidths[(string)column.Header] = column.Width;
         }
         saveProperty(columnWidths);
      }

      private bool _loadingColumnWidth = false;
      private void loadColumnWidths(Dictionary<string, int> storedWidths)
      {
         _loadingColumnWidth = true;
         try
         {
            foreach (TreeColumn column in _treeView.Columns)
            {
               string columnName = (string)column.Header;
               if (storedWidths.ContainsKey(columnName))
               {
                  column.Width = storedWidths[columnName];
               }
            }
         }
         finally
         {
            _loadingColumnWidth = false;
         }
      }
   }
}

