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

         _radioButtonGroup.AddRadioButton(radioButtonSortDefault, (int)(DiscussionSortState.Default));
         _radioButtonGroup.AddRadioButton(radioButtonSortByReviewer, (int)(DiscussionSortState.ByReviewer));
         _radioButtonGroup.UpdateCheckedState((int)initialSort);

         radioButtonSortDefault.CheckedChanged += new System.EventHandler(this.SortElement_CheckedChanged);
         radioButtonSortByReviewer.CheckedChanged += new System.EventHandler(this.SortElement_CheckedChanged);
      }

      /// <summary>
      /// Current state of UI controls
      /// </summary>
      public DiscussionSortState SortState
      {
         get
         {
            return _radioButtonGroup.GetState<DiscussionSortState>();
         }
      }

      private void SortElement_CheckedChanged(object sender, EventArgs e)
      {
         if (sender is RadioButton radioButton && radioButton.Checked || sender is CheckBox checkBox)
         {
            _onSortChanged();
         }
      }

      private readonly Action _onSortChanged;

      private readonly RadioButtonGroup _radioButtonGroup = new RadioButtonGroup();
   }
}

