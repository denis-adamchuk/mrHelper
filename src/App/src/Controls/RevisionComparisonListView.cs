using System;
using System.Windows.Forms;
using System.Collections.Generic;
using mrHelper.StorageSupport;
using mrHelper.CommonControls.Controls;
using System.Diagnostics;

namespace mrHelper.App.Controls
{
   internal class RevisionComparisonListView : ListViewEx
   {
      internal void SetData(ComparisonEx.Statistic statistic)
      {
         ClearData();
         foreach (ComparisonEx.Statistic.Item statisticItem in statistic.Data)
         {
            ListViewItem item = createListViewItem(statisticItem);
            Items.Add(item);
         }
      }

      internal void ClearData()
      {
         Items.Clear();
      }

      private bool _processingHandleCreated = false;
      protected override void OnHandleCreated(EventArgs e)
      {
         _processingHandleCreated = true;
         try
         {
            base.OnHandleCreated(e);
         }
         finally
         {
            _processingHandleCreated = false;
         }
         restoreColumns();
      }

      protected override void OnVisibleChanged(EventArgs e)
      {
         base.OnVisibleChanged(e);
         if (Visible)
         {
            restoreColumns();
         }
      }

      protected override void OnColumnWidthChanged(ColumnWidthChangedEventArgs e)
      {
         base.OnColumnWidthChanged(e);
         if (!_restoringColumns && ! _processingHandleCreated)
         {
            saveColumnWidths();
         }
      }

      private ListViewItem createListViewItem(ComparisonEx.Statistic.Item statisticItem)
      {
         string name = statisticItem.New_Path ?? "";
         if (!String.IsNullOrEmpty(statisticItem.Old_Path) && name != statisticItem.Old_Path)
         {
            name += String.Format("(was {0})", statisticItem.Old_Path);
         }
         string[] subitems = new string[]
         {
            name, statisticItem.Added.ToString(), statisticItem.Deleted.ToString()
         };
         Debug.Assert(subitems.Length == Columns.Count);
         return new ListViewItem(subitems);
      }

      private void setColumnWidths(Dictionary<string, int> widths)
      {
         foreach (ColumnHeader column in Columns)
         {
            string columnName = (string)column.Tag;
            if (widths.ContainsKey(columnName))
            {
               column.Width = widths[columnName];
            }
         }
      }

      private void saveColumnWidths()
      {
         Dictionary<string, int> columnWidths = new Dictionary<string, int>();
         foreach (ColumnHeader column in Columns)
         {
            columnWidths[(string)column.Tag] = column.Width;
         }
         if (Program.Settings != null)
         {
            Program.Settings.RevisionComparisonColumnWidths = columnWidths;
         }
      }

      bool _restoringColumns = false;
      private void restoreColumns()
      {
         _restoringColumns = true;
         try
         {
            Dictionary<string, int> widths = Program.Settings == null
               ? null : Program.Settings.RevisionComparisonColumnWidths;
            if (widths != null)
            {
               setColumnWidths(widths);
            }
         }
         finally
         {
            _restoringColumns = false;
         }
      }
   }
}

