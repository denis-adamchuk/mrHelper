using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.CustomActions;
using mrHelper.GitLabClient;
using System;
using System.Linq;
using System.Windows.Forms;

namespace mrHelper.App.Controls
{
   public partial class DiscussionActionsPanel : UserControl, ICommandCallback
   {
      public DiscussionActionsPanel(Action onRefresh, Action onAddComment, Action onAddThread,
         MergeRequestKey mrk, IHostProperties hostProperties)
      {
         InitializeComponent();
         _onRefresh = onRefresh;
         _onAddComment = onAddComment;
         _onAddThread = onAddThread;
         _mergeRequestKey = mrk;
         _hostProperties = hostProperties;
         addCustomActions();
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

      private void addCustomActions()
      {
         CustomCommandLoader loader = new CustomCommandLoader(this);
         System.Collections.Generic.IEnumerable<ICommand> commands = null;
         try
         {
            string CustomActionsFileName = "CustomActions.xml";
            commands = loader.LoadCommands(CustomActionsFileName);
         }
         catch (CustomCommandLoaderException ex)
         {
            // If file doesn't exist the loader throws, leaving the app in an undesirable state.
            // Do not try to load custom actions if they don't exist.
            ExceptionHandlers.Handle("Cannot load custom actions", ex);
         }

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
            button.Click += async (x, y) =>
            {
               try
               {
                  await command.Run();
                  _onRefresh();
               }
               catch (Exception ex) // Whatever happened in Run()
               {
                  string errorMessage = "Custom action failed";
                  ExceptionHandlers.Handle(errorMessage, ex);
                  MessageBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                  return;
               }
            };
            tableLayoutPanel1.Controls.Add(button);
            tableLayoutPanel1.SetRow(button, id % tableLayoutPanel1.RowCount);
            tableLayoutPanel1.SetColumn(button, id / tableLayoutPanel1.ColumnCount + 1);
            id++;
         }
      }

      public string GetCurrentHostName()
      {
         return _mergeRequestKey.ProjectKey.HostName;
      }

      public string GetCurrentAccessToken()
      {
         return _hostProperties.GetAccessToken(_mergeRequestKey.ProjectKey.HostName);
      }

      public string GetCurrentProjectName()
      {
         return _mergeRequestKey.ProjectKey.ProjectName;
      }

      public int GetCurrentMergeRequestIId()
      {
         return _mergeRequestKey.IId;
      }

      private readonly Action _onRefresh;
      private readonly Action _onAddComment;
      private readonly Action _onAddThread;
      private readonly MergeRequestKey _mergeRequestKey;
      private readonly IHostProperties _hostProperties;
   }
}
