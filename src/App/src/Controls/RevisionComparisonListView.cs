using System;
using System.Windows.Forms;
using System.Collections.Generic;
using mrHelper.StorageSupport;
using mrHelper.CommonControls.Controls;
using System.Diagnostics;
using System.Drawing;
using mrHelper.CommonControls.Tools;
using mrHelper.Common.Constants;
using mrHelper.App.Helpers;

namespace mrHelper.App.Controls
{
   internal class RevisionComparisonListView : ListViewEx
   {
      public RevisionComparisonListView()
      {
         _toolTip = new ListViewToolTip(this,
            getText, getToolTipText, getFormatFlags, getBounds, (x,y) => false);
         OwnerDraw = true;
      }

      internal void SetData(ComparisonEx.Statistic statistic)
      {
         ClearData();
         foreach (ComparisonEx.Statistic.Item statisticItem in statistic.Data)
         {
            ListViewItem item = createListViewItem(statisticItem);
            Items.Add(item);
            for (int iSubItem = 0; iSubItem < item.SubItems.Count; ++iSubItem)
            {
               item.SubItems[iSubItem].Tag = Columns[iSubItem].Tag;
            }
         }
      }

      internal void ClearData()
      {
         Items.Clear();
      }

      protected override void Dispose(bool disposing)
      {
         _toolTip?.Dispose();
         base.Dispose(disposing);
      }

      protected override void OnMouseLeave(EventArgs e)
      {
         // this callback is called not only when mouse leaves the list view so let's check if we need to cancel tooltip
         _toolTip.CancelIfNeeded(Cursor.Position);

         base.OnMouseLeave(e);
      }

      protected override void OnMouseMove(MouseEventArgs e)
      {
         _toolTip.UpdateOnMouseMove(e.Location);

         base.OnMouseMove(e);
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

      protected override void OnDrawColumnHeader(DrawListViewColumnHeaderEventArgs e)
      {
         if (e.Header.ListView == null)
         {
            return;
         }

         ListViewDrawingHelper.DrawColumnHeaderBackground(e);
         ListViewDrawingHelper.DrawColumnHeaderText(e, e.Font);
      }

      protected override void OnDrawSubItem(DrawListViewSubItemEventArgs e)
      {
         base.OnDrawSubItem(e);

         if (e.Item.ListView == null)
         {
            return; // is being removed
         }

         bool isSelected = e.Item.Selected;
         Color backgroundColor = e.ItemIndex % 2 == 1 
            ? ThemeSupport.StockColors.GetThemeColors().OSThemeColors.Control
            : ThemeSupport.StockColors.GetThemeColors().OSThemeColors.ControlDark;
         backgroundColor = isSelected ? ThemeSupport.StockColors.GetThemeColors().SelectionBackground : backgroundColor;
         WinFormsHelpers.FillRectangle(e, e.Bounds, backgroundColor);

         using (Brush textBrush = new SolidBrush(isSelected
            ? ThemeSupport.StockColors.GetThemeColors().OSThemeColors.TextInSelection
            : ThemeSupport.StockColors.GetThemeColors().OSThemeColors.TextActive))
         {
            StringFormat format = new StringFormat(StringFormatFlags.NoWrap)
            {
               Trimming = StringTrimming.EllipsisCharacter
            };
            e.Graphics.DrawString(e.SubItem.Text, Font, textBrush, e.Bounds, format);
         }
      }

      private ListViewItem createListViewItem(ComparisonEx.Statistic.Item statisticItem)
      {
         string filepath = statisticItem.New_Path ?? "";
         if (!String.IsNullOrEmpty(statisticItem.Old_Path) && filepath != statisticItem.Old_Path)
         {
            filepath += String.Format(" (was {0})", statisticItem.Old_Path);
         }
         filepath = filepath.Replace("/", " / ");
         string[] subitems = new string[]
         {
            filepath,
            statisticItem.DiffSize?.Added.ToString() ?? Constants.NoDataAtGitLab,
            statisticItem.DiffSize?.Deleted.ToString() ?? String.Empty
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

      private int getColumnWidth(string columnTag)
      {
         foreach (ColumnHeader column in Columns)
         {
            if (column.Tag.ToString() == columnTag)
            {
               return column.Width;
            }
         }
         return 0;
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
            Dictionary<string, int> widths = Program.Settings?.RevisionComparisonColumnWidths;
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

      private string getToolTipText(ListViewItem.ListViewSubItem subItem)
      {
         return getText(subItem);
      }

      private string getText(ListViewItem.ListViewSubItem subItem)
      {
         return subItem.Text;
      }

      private StringFormatFlags getFormatFlags(ListViewItem.ListViewSubItem subItem)
      {
         return StringFormatFlags.NoWrap;
      }

      private Rectangle getBounds(ListViewItem.ListViewSubItem subItem)
      {
         var width = getColumnWidth(subItem.Tag.ToString());
         return new Rectangle(subItem.Bounds.X, subItem.Bounds.Y, width, subItem.Bounds.Height);
      }

      private readonly ListViewToolTip _toolTip;
   }
}

