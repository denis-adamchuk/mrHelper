using mrHelper.App.Controls;

namespace mrHelper.App.Forms
{
   partial class EditOrderedListViewForm
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
      }

      #region Windows Form Designer generated code

      /// <summary>
      /// Required method for Designer support - do not modify
      /// the contents of this method with the code editor.
      /// </summary>
      private void InitializeComponent()
      {
         this.listView = new mrHelper.App.Controls.StringToBooleanListView();
         this.columnHeaderName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.buttonAddItem = new System.Windows.Forms.Button();
         this.buttonRemoveItem = new System.Windows.Forms.Button();
         this.buttonToggleState = new System.Windows.Forms.Button();
         this.buttonOK = new System.Windows.Forms.Button();
         this.buttonCancel = new mrHelper.CommonControls.Controls.ConfirmCancelButton();
         this.buttonUp = new System.Windows.Forms.Button();
         this.buttonDown = new System.Windows.Forms.Button();
         this.labelChecking = new System.Windows.Forms.Label();
         this.SuspendLayout();
         // 
         // listView
         // 
         this.listView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.listView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderName});
         this.listView.FullRowSelect = true;
         this.listView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
         this.listView.HideSelection = false;
         this.listView.Location = new System.Drawing.Point(12, 12);
         this.listView.MultiSelect = false;
         this.listView.Name = "listView";
         this.listView.OwnerDraw = true;
         this.listView.ShowGroups = false;
         this.listView.Size = new System.Drawing.Size(212, 236);
         this.listView.TabIndex = 0;
         this.listView.UseCompatibleStateImageBehavior = false;
         this.listView.View = System.Windows.Forms.View.Details;
         this.listView.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.listView_ItemSelectionChanged);
         this.listView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.listView_KeyDown);
         // 
         // columnHeaderName
         // 
         this.columnHeaderName.Text = "Name";
         this.columnHeaderName.Width = 205;
         // 
         // buttonAddItem
         // 
         this.buttonAddItem.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonAddItem.Location = new System.Drawing.Point(230, 12);
         this.buttonAddItem.Name = "buttonAddItem";
         this.buttonAddItem.Size = new System.Drawing.Size(75, 23);
         this.buttonAddItem.TabIndex = 1;
         this.buttonAddItem.Text = "Add...";
         this.buttonAddItem.UseVisualStyleBackColor = true;
         this.buttonAddItem.Click += new System.EventHandler(this.buttonAddItem_Click);
         // 
         // buttonRemoveItem
         // 
         this.buttonRemoveItem.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonRemoveItem.Enabled = false;
         this.buttonRemoveItem.Location = new System.Drawing.Point(230, 41);
         this.buttonRemoveItem.Name = "buttonRemoveItem";
         this.buttonRemoveItem.Size = new System.Drawing.Size(75, 23);
         this.buttonRemoveItem.TabIndex = 2;
         this.buttonRemoveItem.Text = "Remove";
         this.buttonRemoveItem.UseVisualStyleBackColor = true;
         this.buttonRemoveItem.Click += new System.EventHandler(this.buttonRemoveItem_Click);
         // 
         // buttonToggleState
         // 
         this.buttonToggleState.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonToggleState.Enabled = false;
         this.buttonToggleState.Location = new System.Drawing.Point(230, 70);
         this.buttonToggleState.Name = "buttonToggleState";
         this.buttonToggleState.Size = new System.Drawing.Size(75, 23);
         this.buttonToggleState.TabIndex = 3;
         this.buttonToggleState.Text = "Enable";
         this.buttonToggleState.UseVisualStyleBackColor = true;
         this.buttonToggleState.Click += new System.EventHandler(this.buttonToggleState_Click);
         // 
         // buttonOK
         // 
         this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
         this.buttonOK.Location = new System.Drawing.Point(230, 196);
         this.buttonOK.Name = "buttonOK";
         this.buttonOK.Size = new System.Drawing.Size(75, 23);
         this.buttonOK.TabIndex = 6;
         this.buttonOK.Text = "OK";
         this.buttonOK.UseVisualStyleBackColor = true;
         // 
         // buttonCancel
         // 
         this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonCancel.ConfirmationText = "All changes will be lost, are you sure?";
         this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
         this.buttonCancel.Location = new System.Drawing.Point(230, 225);
         this.buttonCancel.Name = "buttonCancel";
         this.buttonCancel.Size = new System.Drawing.Size(75, 23);
         this.buttonCancel.TabIndex = 7;
         this.buttonCancel.Text = "Cancel";
         this.buttonCancel.UseVisualStyleBackColor = true;
         // 
         // buttonUp
         // 
         this.buttonUp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonUp.Enabled = false;
         this.buttonUp.Location = new System.Drawing.Point(230, 119);
         this.buttonUp.Name = "buttonUp";
         this.buttonUp.Size = new System.Drawing.Size(75, 23);
         this.buttonUp.TabIndex = 4;
         this.buttonUp.Text = "Up";
         this.buttonUp.UseVisualStyleBackColor = true;
         this.buttonUp.Click += new System.EventHandler(this.buttonUp_Click);
         // 
         // buttonDown
         // 
         this.buttonDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonDown.Enabled = false;
         this.buttonDown.Location = new System.Drawing.Point(230, 148);
         this.buttonDown.Name = "buttonDown";
         this.buttonDown.Size = new System.Drawing.Size(75, 23);
         this.buttonDown.TabIndex = 5;
         this.buttonDown.Text = "Down";
         this.buttonDown.UseVisualStyleBackColor = true;
         this.buttonDown.Click += new System.EventHandler(this.buttonDown_Click);
         // 
         // labelChecking
         // 
         this.labelChecking.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
         this.labelChecking.AutoSize = true;
         this.labelChecking.Location = new System.Drawing.Point(243, 180);
         this.labelChecking.Name = "labelChecking";
         this.labelChecking.Size = new System.Drawing.Size(61, 13);
         this.labelChecking.TabIndex = 8;
         this.labelChecking.Text = "Checking...";
         this.labelChecking.Visible = false;
         // 
         // EditOrderedListViewForm
         // 
         this.AcceptButton = this.buttonOK;
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.CancelButton = this.buttonCancel;
         this.ClientSize = new System.Drawing.Size(316, 260);
         this.Controls.Add(this.labelChecking);
         this.Controls.Add(this.buttonDown);
         this.Controls.Add(this.buttonUp);
         this.Controls.Add(this.buttonCancel);
         this.Controls.Add(this.buttonOK);
         this.Controls.Add(this.buttonToggleState);
         this.Controls.Add(this.buttonRemoveItem);
         this.Controls.Add(this.buttonAddItem);
         this.Controls.Add(this.listView);
         this.Icon = global::mrHelper.App.Properties.Resources.DefaultAppIcon;
         this.MaximizeBox = false;
         this.MinimizeBox = false;
         this.MinimumSize = new System.Drawing.Size(332, 299);
         this.Name = "EditOrderedListViewForm";
         this.Text = "Edit Items";
         this.ResumeLayout(false);
         this.PerformLayout();

      }

        #endregion

        private StringToBooleanListView listView;
        private System.Windows.Forms.Button buttonAddItem;
        private System.Windows.Forms.Button buttonRemoveItem;
        private System.Windows.Forms.Button buttonToggleState;
        private System.Windows.Forms.ColumnHeader columnHeaderName;
      private System.Windows.Forms.Button buttonOK;
      private CommonControls.Controls.ConfirmCancelButton buttonCancel;
      private System.Windows.Forms.Button buttonUp;
      private System.Windows.Forms.Button buttonDown;
      private System.Windows.Forms.Label labelChecking;
   }
}