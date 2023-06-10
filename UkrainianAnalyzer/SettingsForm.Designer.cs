namespace UkrainianAnalyzer
{
    partial class SettingsForm
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
            this.checkedListBox1 = new System.Windows.Forms.CheckedListBox();
            this.SpecialButtonTxt = new System.Windows.Forms.TextBox();
            this.RegularButtonTxt = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.Label = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // checkedListBox1
            // 
            this.checkedListBox1.Font = new System.Drawing.Font("Segoe UI Black", 19F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.checkedListBox1.FormattingEnabled = true;
            this.checkedListBox1.Items.AddRange(new object[] {
            "ukr",
            "eng",
            "rus"});
            this.checkedListBox1.Location = new System.Drawing.Point(12, 12);
            this.checkedListBox1.Name = "checkedListBox1";
            this.checkedListBox1.Size = new System.Drawing.Size(120, 115);
            this.checkedListBox1.TabIndex = 0;
            this.checkedListBox1.SelectedIndexChanged += new System.EventHandler(this.checkedListBox1_SelectedIndexChanged);
            // 
            // SpecialButtonTxt
            // 
            this.SpecialButtonTxt.Location = new System.Drawing.Point(138, 61);
            this.SpecialButtonTxt.Name = "SpecialButtonTxt";
            this.SpecialButtonTxt.Size = new System.Drawing.Size(131, 23);
            this.SpecialButtonTxt.TabIndex = 1;
            this.SpecialButtonTxt.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.SpecialButtonTxt_KeyPress);
            // 
            // RegularButtonTxt
            // 
            this.RegularButtonTxt.Location = new System.Drawing.Point(138, 102);
            this.RegularButtonTxt.Name = "RegularButtonTxt";
            this.RegularButtonTxt.Size = new System.Drawing.Size(131, 23);
            this.RegularButtonTxt.TabIndex = 2;
            this.RegularButtonTxt.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.RegularButtonTxt_KeyPress);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(138, 46);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(89, 12);
            this.label1.TabIndex = 3;
            this.label1.Text = "Special button";
            // 
            // Label
            // 
            this.Label.AutoSize = true;
            this.Label.Location = new System.Drawing.Point(138, 87);
            this.Label.Name = "Label";
            this.Label.Size = new System.Drawing.Size(89, 12);
            this.Label.TabIndex = 4;
            this.Label.Text = "Regular button";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Segoe UI Black", 11F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.label3.Location = new System.Drawing.Point(141, 12);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(118, 20);
            this.label3.TabIndex = 5;
            this.label3.Text = "Hotkey change";
            this.label3.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(271, 137);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.Label);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.RegularButtonTxt);
            this.Controls.Add(this.SpecialButtonTxt);
            this.Controls.Add(this.checkedListBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "SettingsForm";
            this.Text = "SettingsForm";
            this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SettingsForm_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private CheckedListBox checkedListBox1;
        private TextBox SpecialButtonTxt;
        private TextBox RegularButtonTxt;
        private Label label1;
        private Label Label;
        private Label label3;
    }
}