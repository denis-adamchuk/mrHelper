using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using mrHelper.App.Helpers;

namespace mrHelper.App.Controls
{
   public partial class DiscussionSortPanel : UserControl
   {
      public DiscussionSortPanel(DiscussionSortState initialSort, Action onSortChanged)
      {
         _onSortChanged = onSortChanged;

         InitializeComponent();
         bindRadioToFlags();

         setSort(_radioToFlags, (int)initialSort);

         subscribeToEvents();
      }

      /// <summary>
      /// Current state of UI controls
      /// </summary>
      public DiscussionSortState SortState
      {
         get
         {
            return getSort<DiscussionSortState>(_radioToFlags);
         }
      }

      private void SortElement_CheckedChanged(object sender, EventArgs e)
      {
         if (sender is RadioButton radioButton && radioButton.Checked || sender is CheckBox checkBox)
         {
            _onSortChanged();
         }
      }

      private void bindRadioToFlags()
      {
         _radioToFlags.Add(radioButtonSortDefault, (int)(DiscussionSortState.Default));
         _radioToFlags.Add(radioButtonSortByAuthor, (int)(DiscussionSortState.ByAuthor));
      }

      private static void setSort(Dictionary<RadioButton, int> flags, int value)
      {
         foreach (var radio in flags)
         {
            radio.Key.Checked = radio.Value == (int)value;
         }
      }

      private static T getSort<T>(Dictionary<RadioButton, int> flags)
      {
         foreach (var radio in flags)
         {
            if (radio.Key.Checked)
            {
               return (T)(object)radio.Value;
            }
         }

         Debug.Assert(false);
         return (T)(object)0;
      }

      private readonly Action _onSortChanged;

      private readonly Dictionary<RadioButton, int> _radioToFlags = new Dictionary<RadioButton, int>();
   }
}

