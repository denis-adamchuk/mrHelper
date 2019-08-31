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

         checkBoxCreatedByMe.Checked = initialFilter.ByCurrentUserOnly;
         if (initialFilter.ByAnswers.HasFlag(FilterByAnswers.Answered)
          && initialFilter.ByAnswers.HasFlag(FilterByAnswers.Unanswered))
         {
            radioButtonShowAll.Checked = true;
         }
         else if (initialFilter.ByAnswers.HasFlag(FilterByAnswers.Answered))
         {
            radioButtonShowAnsweredOnly.Checked = true;
         }
         else if (initialFilter.ByAnswers.HasFlag(FilterByAnswers.Unanswered))
         {
            radioButtonShowUnansweredOnly.Checked = true;
         }
      }

      /// <summary>
      /// Current state of UI controls
      /// </summary>
      public DiscussionFilterState Filter
      {
         get
         {
            FilterByAnswers byAnswers = FilterByAnswers.Answered | FilterByAnswers.Unanswered;
            if (radioButtonShowAnsweredOnly.Checked)
            {
               byAnswers = FilterByAnswers.Answered;
            }
            else if (radioButtonShowUnansweredOnly.Checked)
            {
               byAnswers = FilterByAnswers.Unanswered;
            }
            else
            {
               Debug.Assert(radioButtonShowAll.Checked);
            }

            return new DiscussionFilterState
            {
               ByCurrentUserOnly = checkBoxCreatedByMe.Checked,
               ByAnswers = byAnswers
            };
         }
      }

      private void ButtonApplyFilter_Click(object sender, EventArgs e)
      {
         _onFilterChanged();
      }

      private readonly Action _onFilterChanged;
   }
}

