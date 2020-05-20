using System;
using System.Windows.Forms;
using mrHelper.App.Helpers;
using mrHelper.CommonControls.Tools;

namespace mrHelper.App.Controls
{
   /// <summary>
   /// A filter of discussions used in Discussions Form
   /// </summary>
   public partial class DiscussionFilterPanel : UserControl
   {
      public DiscussionFilterPanel(DiscussionFilterState initialFilter, Action onFilterChanged)
      {
         _onFilterChanged = onFilterChanged;

         InitializeComponent();
         bindRadioToFlags();

         checkBoxCreatedByMe.Checked = initialFilter.ByCurrentUserOnly;
         checkBoxShowService.Checked = initialFilter.ServiceMessages;
         _radioButtonsByAnswers.UpdateCheckedState((int)initialFilter.ByAnswers);
         _radioButtonsByResolution.UpdateCheckedState((int)initialFilter.ByResolution);

         subscribeToEvents();
      }

      private void subscribeToEvents()
      {
         radioButtonShowNotResolvedOnly.CheckedChanged += new System.EventHandler(this.FilterElement_CheckedChanged);
         radioButtonShowResolvedOnly.CheckedChanged += new System.EventHandler(this.FilterElement_CheckedChanged);
         radioButtonNoFilterByResolution.CheckedChanged += new System.EventHandler(this.FilterElement_CheckedChanged);
         radioButtonShowUnansweredOnly.CheckedChanged += new System.EventHandler(this.FilterElement_CheckedChanged);
         radioButtonShowAnsweredOnly.CheckedChanged += new System.EventHandler(this.FilterElement_CheckedChanged);
         radioButtonNoFilterByAnswers.CheckedChanged += new System.EventHandler(this.FilterElement_CheckedChanged);
         checkBoxCreatedByMe.CheckedChanged += new System.EventHandler(this.FilterElement_CheckedChanged);
         checkBoxShowService.CheckedChanged += new System.EventHandler(this.FilterElement_CheckedChanged);
      }

      /// <summary>
      /// Current state of UI controls
      /// </summary>
      public DiscussionFilterState Filter
      {
         get
         {
            return new DiscussionFilterState
            (
               checkBoxCreatedByMe.Checked,
               checkBoxShowService.Checked,
               false, // system notes
               _radioButtonsByAnswers.GetState<FilterByAnswers>(),
               _radioButtonsByResolution.GetState<FilterByResolution>()
            );
         }
      }

      private void FilterElement_CheckedChanged(object sender, EventArgs e)
      {
         if (sender is RadioButton radioButton && radioButton.Checked || sender is CheckBox checkBox)
         {
            _onFilterChanged();
         }
      }

      private void bindRadioToFlags()
      {
         _radioButtonsByAnswers.AddRadioButton(radioButtonNoFilterByAnswers,
            (int)(FilterByAnswers.Answered | FilterByAnswers.Unanswered));
         _radioButtonsByAnswers.AddRadioButton(radioButtonShowAnsweredOnly, (int)FilterByAnswers.Answered);
         _radioButtonsByAnswers.AddRadioButton(radioButtonShowUnansweredOnly, (int)FilterByAnswers.Unanswered);

         _radioButtonsByResolution.AddRadioButton(radioButtonNoFilterByResolution,
            (int)(FilterByResolution.Resolved | FilterByResolution.NotResolved));
         _radioButtonsByResolution.AddRadioButton(radioButtonShowResolvedOnly, (int)FilterByResolution.Resolved);
         _radioButtonsByResolution.AddRadioButton(radioButtonShowNotResolvedOnly, (int)FilterByResolution.NotResolved);
      }

      private readonly Action _onFilterChanged;

      private readonly RadioButtonGroup _radioButtonsByAnswers = new RadioButtonGroup();
      private readonly RadioButtonGroup _radioButtonsByResolution = new RadioButtonGroup();
   }
}

