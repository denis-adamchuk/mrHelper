using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.CustomActions;
using mrHelper.GitLabClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace mrHelper.App.Controls
{
   public partial class DiscussionActionsPanel : UserControl
   {
      public DiscussionActionsPanel(Action onRefresh, Action onAddComment, Action onAddThread,
         IEnumerable<ICommand> commands, Action<ICommand> onCommand)
      {
         InitializeComponent();
         _onRefresh = onRefresh;
         _onAddComment = onAddComment;
         _onAddThread = onAddThread;
         _onCommand = onCommand;
         addCustomActions(commands);
      }

      private void ButtonDiscussionsRefresh_Click(object sender, EventArgs e)
      {
         _onRefresh();
      }

      private void buttonAddComment_Click(object sender, EventArgs e)
      {
         _onAddComment();
      }

      private void buttonNewThread_Click(object sender, EventArgs e)
      {
         _onAddThread();
      }

      private void addCustomActions(IEnumerable<ICommand> commands)
      {
         if (commands == null)
         {
            return;
         }

         int rowCount = tableLayoutPanel1.RowCount;
         tableLayoutPanel1.ColumnCount = 1 + commands.Count() / rowCount  + (commands.Count() % rowCount == 0 ? 0 : 1);
         for (int iColumn = 0; iColumn < tableLayoutPanel1.ColumnCount; ++iColumn)
         {
            tableLayoutPanel1.ColumnStyles[iColumn].SizeType = SizeType.Percent;
            tableLayoutPanel1.ColumnStyles[iColumn].Width = (float)100 / tableLayoutPanel1.ColumnCount;
         }

         int id = 0;
         foreach (ICommand command in commands)
         {
            string name = command.GetName();
            var button = new System.Windows.Forms.Button
            {
               Name = "customAction" + id,
               Location = new System.Drawing.Point { X = 0, Y = 19 },
               Size = new System.Drawing.Size { Width = 72, Height = 32 },
               MinimumSize = new System.Drawing.Size { Width = 72, Height = 0 },
               Text = name,
               UseVisualStyleBackColor = true,
               Enabled = true,
               TabStop = false,
               Dock = DockStyle.Fill
            };
            toolTipActionsPanel.SetToolTip(button, command.GetHint());
            button.Click += (x, y) => _onCommand(command);
            tableLayoutPanel1.Controls.Add(button);
            tableLayoutPanel1.SetRow(button, id % tableLayoutPanel1.RowCount);
            tableLayoutPanel1.SetColumn(button, id / tableLayoutPanel1.ColumnCount + 1);
            id++;
         }
      }

      private readonly Action _onRefresh;
      private readonly Action _onAddComment;
      private readonly Action _onAddThread;
      private readonly Action<ICommand> _onCommand;
   }
}
