using System;
using System.Drawing;
using System.Windows.Forms;

namespace mrHelper.App.Controls
{
   internal class StringToBooleanListView : ListView
   {
      public StringToBooleanListView()
         : base()
      {
         OwnerDraw = true;
         View = System.Windows.Forms.View.Details;
      }

      protected override void OnDrawSubItem(DrawListViewSubItemEventArgs e)
      {
         base.OnDrawSubItem(e);

         if (e.Item.ListView == null)
         {
            return; // is being removed
         }

         Tuple<string, bool> tag = (Tuple<string, bool>)(e.Item.Tag);

         e.DrawBackground();

         bool isSelected = e.Item.Selected;
         if (isSelected)
         {
            e.Graphics.FillRectangle(SystemBrushes.Highlight, e.Bounds);
         }

         string text = tag.Item1;
         Brush textBrush = isSelected ? SystemBrushes.HighlightText :
            (tag.Item2 ? SystemBrushes.ControlText : Brushes.LightGray);

         StringFormat format =
            new StringFormat
            {
               Trimming = StringTrimming.EllipsisCharacter,
               FormatFlags = StringFormatFlags.NoWrap
            };

         e.Graphics.DrawString(text, e.Item.ListView.Font, textBrush, e.Bounds, format);
      }
   }
}

