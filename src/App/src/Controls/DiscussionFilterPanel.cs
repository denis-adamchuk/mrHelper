using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitLabSharp.Entities;
using System.Diagnostics;
using mrHelper.App.Helpers;

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
         setFilter(_byAnswersRadioToFlags, (int)initialFilter.ByAnswers);
         setFilter(_byResolutionRadioToFlags, (int)initialFilter.ByResolution);

         subscribeToEvents();
      }

      /// <summary>
      /// Current state of UI controls
      /// </summary>
      public DiscussionFilterState Filter
      {
         get
         {
            return new DiscussionFilterState
            {
               ByCurrentUserOnly = checkBoxCreatedByMe.Checked,
               ByAnswers = getFilter<FilterByAnswers>(_byAnswersRadioToFlags),
               ByResolution = getFilter<FilterByResolution>(_byResolutionRadioToFlags)
            };
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
         _byAnswersRadioToFlags.Add(radioButtonNoFilterByAnswers,
            (int)(FilterByAnswers.Answered | FilterByAnswers.Unanswered));
         _byAnswersRadioToFlags.Add(radioButtonShowAnsweredOnly, (int)FilterByAnswers.Answered);
         _byAnswersRadioToFlags.Add(radioButtonShowUnansweredOnly, (int)FilterByAnswers.Unanswered);

         _byResolutionRadioToFlags.Add(radioButtonNoFilterByResolution,
            (int)(FilterByResolution.Resolved | FilterByResolution.NotResolved));
         _byResolutionRadioToFlags.Add(radioButtonShowResolvedOnly, (int)FilterByResolution.Resolved);
         _byResolutionRadioToFlags.Add(radioButtonShowNotResolvedOnly, (int)FilterByResolution.NotResolved);
      }

      private static void setFilter(Dictionary<RadioButton, int> flags, int value)
      {
         foreach (var radio in flags)
         {
            radio.Key.Checked = radio.Value == (int)value;
         }
      }

      private static T getFilter<T>(Dictionary<RadioButton, int> flags)
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

      private readonly Action _onFilterChanged;

      private readonly Dictionary<RadioButton, int> _byAnswersRadioToFlags = new Dictionary<RadioButton, int>();
      private readonly Dictionary<RadioButton, int> _byResolutionRadioToFlags = new Dictionary<RadioButton, int>();
   }
}

