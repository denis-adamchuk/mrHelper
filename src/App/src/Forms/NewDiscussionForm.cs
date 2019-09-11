using System.Drawing;
using System.Windows.Forms;
using TheArtOfDev.HtmlRenderer.WinForms;
using mrHelper.Common.Interfaces;
using mrHelper.Core.Context;
using mrHelper.Core.Interprocess;
using mrHelper.Core.Matching;

namespace mrHelper.App.Forms
{
   internal partial class NewDiscussionForm : Form
   {
      /// <summary>
      /// Throws GitOperationException in case of problems with git.
      /// </summary>
      internal NewDiscussionForm(Snapshot snapshot, DiffToolInfo difftoolInfo, DiffPosition position,
         IGitRepository gitRepository)
      {
         _interprocessSnapshot = snapshot;
         _difftoolInfo = difftoolInfo;
         _position = position;
         _gitRepository = gitRepository;

         InitializeComponent();
         htmlPanel.BorderStyle = BorderStyle.FixedSingle;
         htmlPanel.Location = new Point(12, 73);
         htmlPanel.Size = new Size(860, 76);
         Controls.Add(htmlPanel);

         this.ActiveControl = textBoxDiscussionBody;
         showDiscussionContext(htmlPanel, textBoxFileName);
      }

      public bool IncludeContext { get { return checkBoxIncludeContext.Checked; } }
      public string Body { get { return textBoxDiscussionBody.Text; } }

      private void TextBoxDiscussionBody_KeyDown(object sender, KeyEventArgs e)
      {
         if (e.KeyCode == Keys.Enter && Control.ModifierKeys == Keys.Control)
         {
            e.Handled = false;

            buttonOK.PerformClick();
         }
      }

      /// <summary>
      /// Throws ArgumentException.
      /// Throws GitOperationException and GitObjectException in case of problems with git.
      /// </summary>
      private void showDiscussionContext(HtmlPanel htmlPanel, TextBox tbFileName)
      {
         ContextDepth depth = new ContextDepth(0, 3);
         IContextMaker textContextMaker = new EnhancedContextMaker(_gitRepository);
         DiffContext context = textContextMaker.GetContext(_position, depth);

         DiffContextFormatter formatter = new DiffContextFormatter();
         htmlPanel.Text = formatter.FormatAsHTML(context);

         tbFileName.Text = "Left: " + (_difftoolInfo.Left?.FileName ?? "N/A")
                      + "  Right: " + (_difftoolInfo.Right?.FileName ?? "N/A");
      }

      private readonly Snapshot _interprocessSnapshot;
      private readonly DiffToolInfo _difftoolInfo;
      private readonly DiffPosition _position;
      private readonly IGitRepository _gitRepository;
   }
}

