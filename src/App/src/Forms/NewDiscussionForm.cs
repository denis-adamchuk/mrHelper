using System;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading.Tasks;
using mrHelper.Core.Context;
using mrHelper.Core.Matching;
using mrHelper.Common.Constants;
using mrHelper.Common.Interfaces;
using mrHelper.CommonNative;
using mrHelper.Common.Exceptions;

namespace mrHelper.App.Forms
{
   internal partial class NewDiscussionForm : CustomFontForm
   {
      internal NewDiscussionForm()
      {
         InitializeComponent();
         htmlPanel.BorderStyle = BorderStyle.FixedSingle;
         htmlPanel.Location = new Point(12, 73);
         htmlPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
         Controls.Add(htmlPanel);
      }

      internal void Initialize(string leftSideFileName, string rightSideFileName,
         DiffPosition position, IGitRepository gitRepository)
      {
         applyFont(Program.Settings.MainWindowFontSizeName);

         this.Text = Constants.NewDiscussionCaption;
         this.ActiveControl = textBoxDiscussionBody;
         showDiscussionContext(leftSideFileName, rightSideFileName, position, gitRepository);

         applyTheme(Program.Settings.VisualThemeName);
      }

      private void applyTheme(string theme)
      {
         if (theme == "New Year 2020")
         {
            pictureBox1.BackgroundImage = mrHelper.App.Properties.Resources.Penguin;
            pictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            pictureBox1.Visible = true;
            htmlPanel.Width = pictureBox1.Location.X - 50 - htmlPanel.Location.X;
         }
         else
         {
            pictureBox1.Visible = false;
            htmlPanel.Width = textBoxDiscussionBody.Width;
         }
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

      private void NewDiscussionForm_Shown(object sender, EventArgs e)
      {
         TopMost = false; // disable TopMost property which is initially true
         Win32Tools.ForceWindowIntoForeground(this.Handle);
      }

      /// <summary>
      /// Throws ArgumentException.
      /// </summary>
      private void showDiscussionContext(string leftSideFileName, string rightSideFileName,
         DiffPosition position, IGitRepository gitRepository)
      {
         BeginInvoke(new Action(
            async () => htmlPanel.Text = await getContextHtmlText(position, gitRepository) ), null);

         htmlPanel.Height = htmlPanel.DisplayRectangle.Height + 2;
         textBoxFileName.Text = "Left: " + (leftSideFileName == String.Empty ? "N/A" : leftSideFileName)
                           + "  Right: " + (rightSideFileName == String.Empty ? "N/A" : rightSideFileName);
      }

      async private Task<string> getContextHtmlText(DiffPosition position, IGitRepository gitRepository)
      {
         DiffContext? context;
         try
         {
            ContextDepth depth = new ContextDepth(0, 3);
            IContextMaker textContextMaker = new SimpleContextMaker(gitRepository);
            context = await textContextMaker.GetContext(position, depth);
         }
         catch (ContextMakingException ex)
         {
            string errorMessage = "Cannot render HTML context.";
            ExceptionHandlers.Handle(errorMessage, ex);
            return String.Format("<html><body>{0} See logs for details</body></html>", errorMessage);
         }

         Debug.Assert(context.HasValue);
         DiffContextFormatter formatter = new DiffContextFormatter();
         return formatter.FormatAsHTML(context.Value, htmlPanel.Font.Height, 2);
      }
   }
}

