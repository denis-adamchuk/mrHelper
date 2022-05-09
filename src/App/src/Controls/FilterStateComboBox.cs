using System;
using System.Diagnostics;
using System.Windows.Forms;
using mrHelper.GitLabClient;

namespace mrHelper.App.Controls
{
   internal class FilterStateComboBox : ComboBox
   {
      public FilterStateComboBox()
      {
         foreach (FilterState state in Enum.GetValues(typeof(FilterState)))
         {
            Items.Add(state);
         }
      }

      public FilterState GetSelected()
      {
         return (FilterState)SelectedItem;
      }

      public void Select(FilterState filterState)
      {
         foreach (object item in Items)
         {
            if ((FilterState)item == filterState)
            {
               SelectedItem = item;
               return;
            }
         }
         Debug.Assert(false);
      }

      protected override void OnFormat(ListControlConvertEventArgs e)
      {
         e.Value = getString((FilterState)e.ListItem);
         base.OnFormat(e);
      }

      private static string getString(FilterState filterState)
      {
         switch (filterState)
         {
            case FilterState.Enabled:
               return "Show selected";
            case FilterState.Disabled:
               return "Show all";
            case FilterState.ShowHiddenOnly:
               return "Show hidden only";
         }
         Debug.Assert(false);
         return String.Empty;
      }
   }
}

