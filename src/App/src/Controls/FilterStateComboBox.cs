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

      public void Initialize(Func<int> fnGetHiddenCount)
      {
         _fnGetHiddenCount = fnGetHiddenCount;
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

      public new void RefreshItems()
      {
         // Refresh text of combo box items
         base.RefreshItems();
      }

      protected override void OnFormat(ListControlConvertEventArgs e)
      {
         e.Value = getString((FilterState)e.ListItem);
         base.OnFormat(e);
      }

      private string getString(FilterState filterState)
      {
         int hiddenCount = _fnGetHiddenCount?.Invoke() ?? 0;
         switch (filterState)
         {
            case FilterState.Disabled:
               return "Show all";

            case FilterState.Enabled:
               {
                  string text = "Matching only";
                  if (hiddenCount == 0)
                  {
                     return text;
                  }
                  return String.Format("{0} (hidden: {1})", text, hiddenCount.ToString());
               }

            case FilterState.ShowHiddenOnly:
               {
                  string text = "Hidden only";
                  return text;
               }
         }
         Debug.Assert(false);
         return String.Empty;
      }

      private Func<int> _fnGetHiddenCount;
   }
}

