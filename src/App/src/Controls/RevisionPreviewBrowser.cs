using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Aga.Controls.Tree;
using Aga.Controls.Tree.NodeControls;
using GitLabSharp.Entities;
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
         _treeView.DrawControl += onTreeViewDrawControl;
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
         _treeView.Font = this.Font;
      }

      private void onTreeViewDrawRow(object sender, TreeViewRowDrawEventArgs e)
      {
         if (e.Node.IsSelected)
         {
            Rectangle focusRect = new Rectangle(
               _treeView.OffsetX, e.RowRect.Y, _treeView.ClientRectangle.Width, e.RowRect.Height);
            using (Brush brush = new SolidBrush(DarkModeForms.DarkModeCS.GetSystemColors().Accent))
            {
               e.Graphics.FillRectangle(brush, focusRect);
            }
         }
      }

      private void onTreeViewDrawControl(object sender, DrawEventArgs args)
      {
         if (args is DrawTextEventArgs)
         {
            if (args.Node.IsSelected)
            {
               ((DrawTextEventArgs)args).TextColor = DarkModeForms.DarkModeCS.GetSystemColors().TextInAccent;
            }
            else
            {
               ((DrawTextEventArgs)args).TextColor = DarkModeForms.DarkModeCS.GetSystemColors().TextActive;
            }
         }
      }

      private void onTreeViewColumnWidthChanged(object sender, TreeColumnEventArgs e)
      {
         if (!_loadingColumnWidth && !_initializing)
         {
            saveColumnWidths(x => Program.Settings.RevisionPreviewBrowserColumnWidths = x);
         }
      }

      private void onDrawColHeaderBg(object sender, DrawColHeaderBgEventArgs args)
      {
         using (Brush brush = new SolidBrush(DarkModeForms.DarkModeCS.GetSystemColors().ControlLight))
         {
            args.Graphics.FillRectangle(brush, args.Bounds);
         }
         args.Handled = true;
      }

      private void onDrawColHeaderText(object sender, DrawColHeaderTextEventArgs args)
      {
         TextRenderer.DrawText(args.Graphics, args.Text, _treeView.Font, args.Bounds,
            DarkModeForms.DarkModeCS.GetSystemColors().TextActive, args.Flags);
         args.Handled = true;
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

