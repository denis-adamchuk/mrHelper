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
         initializeControlToFlags();

         checkBoxCreatedByMe.Checked = initialFilter.ByCurrentUserOnly;
         setFilter(_byAnswersToFlags, (int)initialFilter.ByAnswers);
         setFilter(_byResolutionToFlags, (int)initialFilter.ByResolution);
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
               ByAnswers = getFilter<FilterByAnswers>(_byAnswersToFlags),
               ByResolution = getFilter<FilterByResolution>(_byResolutionToFlags)
            };
         }
      }

      private void ButtonApplyFilter_Click(object sender, EventArgs e)
      {
         _onFilterChanged();
      }

      private void initializeControlToFlags()
      {
         _byAnswersToFlags.Add(radioButtonNoFilterByAnswers,
            (int)(FilterByAnswers.Answered | FilterByAnswers.Unanswered));
         _byAnswersToFlags.Add(radioButtonShowAnsweredOnly, (int)FilterByAnswers.Answered);
         _byAnswersToFlags.Add(radioButtonShowUnansweredOnly, (int)FilterByAnswers.Unanswered);

         _byResolutionToFlags.Add(radioButtonNoFilterByResolution,
            (int)(FilterByResolution.Resolved | FilterByResolution.NotResolved));
         _byResolutionToFlags.Add(radioButtonShowResolvedOnly, (int)FilterByResolution.Resolved);
         _byResolutionToFlags.Add(radioButtonShowNotResolvedOnly, (int)FilterByResolution.NotResolved);
      }

      private void setFilter(Dictionary<RadioButton, int> flags, int value)
      {
         foreach (var radio in flags)
         {
            radio.Key.Checked = radio.Value == (int)value;
         }
      }

      private T getFilter<T>(Dictionary<RadioButton, int> flags) where T: System.Enum
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

      private Dictionary<RadioButton, int> _byAnswersToFlags = new Dictionary<RadioButton, int>();
      private Dictionary<RadioButton, int> _byResolutionToFlags = new Dictionary<RadioButton, int>();
   }
}

