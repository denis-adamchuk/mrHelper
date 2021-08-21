
namespace mrHelper.App.Controls
{
   internal partial class DescriptionSplitContainerSite
   {
      /// <summary> 
      /// Required designer variable.
      /// </summary>
      private System.ComponentModel.IContainer components = null;

      /// <summary> 
      /// Clean up any resources being used.
      /// </summary>
      /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
      protected override void Dispose(bool disposing)
      {
         if (disposing && (components != null))
         {
            components.Dispose();
         }

         base.Dispose(disposing);
         stopRedrawTimer();
      }

      #region Component Designer generated code

      /// <summary> 
      /// Required method for Designer support - do not modify 
      /// the contents of this method with the code editor.
      /// </summary>
      private void InitializeComponent()
      {
         this.splitContainer = new System.Windows.Forms.SplitContainer();
         this.richTextBoxMergeRequestDescription = new TheArtOfDev.HtmlRenderer.WinForms.HtmlPanel();
         this.htmlPanelAuthorComments = new TheArtOfDev.HtmlRenderer.WinForms.HtmlPanel();
         ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
         this.splitContainer.Panel1.SuspendLayout();
         this.splitContainer.Panel2.SuspendLayout();
         this.splitContainer.SuspendLayout();
         this.SuspendLayout();
         // 
         // splitContainer
         // 
         this.splitContainer.BackColor = System.Drawing.Color.LightGray;
         this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
         this.splitContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
         this.splitContainer.Location = new System.Drawing.Point(0, 0);
         this.splitContainer.Name = "splitContainer";
         this.splitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
         // 
         // splitContainer.Panel1
         // 
         this.splitContainer.Panel1.BackColor = System.Drawing.SystemColors.Window;
         this.splitContainer.Panel1.Controls.Add(this.richTextBoxMergeRequestDescription);
         // 
         // splitContainer.Panel2
         // 
         this.splitContainer.Panel2.BackColor = System.Drawing.SystemColors.Window;
         this.splitContainer.Panel2.Controls.Add(this.htmlPanelAuthorComments);
         this.splitContainer.Size = new System.Drawing.Size(396, 374);
         this.splitContainer.SplitterDistance = 124;
         this.splitContainer.TabIndex = 0;
         // 
         // richTextBoxMergeRequestDescription
         // 
         this.richTextBoxMergeRequestDescription.AutoScroll = true;
         this.richTextBoxMergeRequestDescription.BackColor = System.Drawing.SystemColors.Window;
         this.richTextBoxMergeRequestDescription.BaseStylesheet = null;
         this.richTextBoxMergeRequestDescription.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
         this.richTextBoxMergeRequestDescription.Dock = System.Windows.Forms.DockStyle.Fill;
         this.richTextBoxMergeRequestDescription.Location = new System.Drawing.Point(0, 0);
         this.richTextBoxMergeRequestDescription.Name = "richTextBoxMergeRequestDescription";
         this.richTextBoxMergeRequestDescription.Size = new System.Drawing.Size(396, 124);
         this.richTextBoxMergeRequestDescription.TabIndex = 3;
         this.richTextBoxMergeRequestDescription.TabStop = false;
         this.richTextBoxMergeRequestDescription.Text = null;
         // 
         // htmlPanelAuthorComments
         // 
         this.htmlPanelAuthorComments.AutoScroll = true;
         this.htmlPanelAuthorComments.BackColor = System.Drawing.SystemColors.Window;
         this.htmlPanelAuthorComments.BaseStylesheet = null;
         this.htmlPanelAuthorComments.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
         this.htmlPanelAuthorComments.Cursor = System.Windows.Forms.Cursors.IBeam;
         this.htmlPanelAuthorComments.Dock = System.Windows.Forms.DockStyle.Fill;
         this.htmlPanelAuthorComments.Location = new System.Drawing.Point(0, 0);
         this.htmlPanelAuthorComments.Name = "htmlPanelAuthorComments";
         this.htmlPanelAuthorComments.Size = new System.Drawing.Size(396, 246);
         this.htmlPanelAuthorComments.TabIndex = 1;
         this.htmlPanelAuthorComments.TabStop = false;
         this.htmlPanelAuthorComments.Text = null;
         // 
         // DescriptionSplitContainerSite
         // 
         this.Controls.Add(this.splitContainer);
         this.Name = "DescriptionSplitContainerSite";
         this.Size = new System.Drawing.Size(396, 374);
         this.splitContainer.Panel1.ResumeLayout(false);
         this.splitContainer.Panel2.ResumeLayout(false);
         ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
         this.splitContainer.ResumeLayout(false);
         this.ResumeLayout(false);

      }

      #endregion

      private System.Windows.Forms.SplitContainer splitContainer;
      private TheArtOfDev.HtmlRenderer.WinForms.HtmlPanel richTextBoxMergeRequestDescription;
      private TheArtOfDev.HtmlRenderer.WinForms.HtmlPanel htmlPanelAuthorComments;
   }
}
