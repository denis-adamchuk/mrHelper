namespace mrHelper.App.Forms
{
   partial class EditTimeForm
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
         System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EditTimeForm));
         this.labelH = new System.Windows.Forms.Label();
         this.labelM = new System.Windows.Forms.Label();
         this.labelS = new System.Windows.Forms.Label();
         this.buttonOK = new System.Windows.Forms.Button();
         this.buttonCancel = new System.Windows.Forms.Button();
         this.numericUpDownH = new System.Windows.Forms.NumericUpDown();
         this.numericUpDownM = new System.Windows.Forms.NumericUpDown();
         this.numericUpDownS = new System.Windows.Forms.NumericUpDown();
         ((System.ComponentModel.ISupportInitialize)(this.numericUpDownH)).BeginInit();
         ((System.ComponentModel.ISupportInitialize)(this.numericUpDownM)).BeginInit();
         ((System.ComponentModel.ISupportInitialize)(this.numericUpDownS)).BeginInit();
         this.SuspendLayout();
         // 
         // labelH
         // 
         this.labelH.AutoSize = true;
         this.labelH.Location = new System.Drawing.Point(60, 15);
         this.labelH.Name = "labelH";
         this.labelH.Size = new System.Drawing.Size(13, 13);
         this.labelH.TabIndex = 3;
         this.labelH.Text = "h";
         // 
         // labelM
         // 
         this.labelM.AutoSize = true;
         this.labelM.Location = new System.Drawing.Point(127, 15);
         this.labelM.Name = "labelM";
         this.labelM.Size = new System.Drawing.Size(15, 13);
         this.labelM.TabIndex = 4;
         this.labelM.Text = "m";
         // 
         // labelS
         // 
         this.labelS.AutoSize = true;
         this.labelS.Location = new System.Drawing.Point(196, 15);
         this.labelS.Name = "labelS";
         this.labelS.Size = new System.Drawing.Size(12, 13);
         this.labelS.TabIndex = 5;
         this.labelS.Text = "s";
         // 
         // buttonOK
         // 
         this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
         this.buttonOK.Location = new System.Drawing.Point(12, 47);
         this.buttonOK.Name = "buttonOK";
         this.buttonOK.Size = new System.Drawing.Size(75, 23);
         this.buttonOK.TabIndex = 3;
         this.buttonOK.Text = "OK";
         this.buttonOK.UseVisualStyleBackColor = true;
         // 
         // buttonCancel
         // 
         this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
         this.buttonCancel.Location = new System.Drawing.Point(133, 47);
         this.buttonCancel.Name = "buttonCancel";
         this.buttonCancel.Size = new System.Drawing.Size(75, 23);
         this.buttonCancel.TabIndex = 4;
         this.buttonCancel.Text = "Cancel";
         this.buttonCancel.UseVisualStyleBackColor = true;
         // 
         // numericUpDownH
         // 
         this.numericUpDownH.Location = new System.Drawing.Point(12, 13);
         this.numericUpDownH.Maximum = new decimal(new int[] {
            59,
            0,
            0,
            0});
         this.numericUpDownH.Name = "numericUpDownH";
         this.numericUpDownH.Size = new System.Drawing.Size(42, 20);
         this.numericUpDownH.TabIndex = 6;
         this.numericUpDownH.KeyDown += new System.Windows.Forms.KeyEventHandler(NumericUpDown_KeyDown);
         // 
         // numericUpDownM
         // 
         this.numericUpDownM.Location = new System.Drawing.Point(79, 12);
         this.numericUpDownM.Maximum = new decimal(new int[] {
            59,
            0,
            0,
            0});
         this.numericUpDownM.Name = "numericUpDownM";
         this.numericUpDownM.Size = new System.Drawing.Size(42, 20);
         this.numericUpDownM.TabIndex = 7;
         this.numericUpDownM.KeyDown += new System.Windows.Forms.KeyEventHandler(NumericUpDown_KeyDown);
         // 
         // numericUpDownS
         // 
         this.numericUpDownS.Location = new System.Drawing.Point(148, 13);
         this.numericUpDownS.Maximum = new decimal(new int[] {
            59,
            0,
            0,
            0});
         this.numericUpDownS.Name = "numericUpDownS";
         this.numericUpDownS.Size = new System.Drawing.Size(42, 20);
         this.numericUpDownS.TabIndex = 8;
         this.numericUpDownS.KeyDown += new System.Windows.Forms.KeyEventHandler(NumericUpDown_KeyDown);
         // 
         // EditTimeForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.CancelButton = this.buttonCancel;
         this.ClientSize = new System.Drawing.Size(215, 82);
         this.Controls.Add(this.numericUpDownS);
         this.Controls.Add(this.numericUpDownM);
         this.Controls.Add(this.numericUpDownH);
         this.Controls.Add(this.buttonCancel);
         this.Controls.Add(this.buttonOK);
         this.Controls.Add(this.labelS);
         this.Controls.Add(this.labelM);
         this.Controls.Add(this.labelH);
         this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
         this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
         this.MaximizeBox = false;
         this.MinimizeBox = false;
         this.Name = "EditTimeForm";
         this.Text = "Edit Spent Time";
         ((System.ComponentModel.ISupportInitialize)(this.numericUpDownH)).EndInit();
         ((System.ComponentModel.ISupportInitialize)(this.numericUpDownM)).EndInit();
         ((System.ComponentModel.ISupportInitialize)(this.numericUpDownS)).EndInit();
         this.ResumeLayout(false);
         this.PerformLayout();

      }

      #endregion
      private System.Windows.Forms.Label labelH;
      private System.Windows.Forms.Label labelM;
      private System.Windows.Forms.Label labelS;
      private System.Windows.Forms.Button buttonOK;
      private System.Windows.Forms.Button buttonCancel;
      private System.Windows.Forms.NumericUpDown numericUpDownH;
      private System.Windows.Forms.NumericUpDown numericUpDownM;
      private System.Windows.Forms.NumericUpDown numericUpDownS;
   }
}