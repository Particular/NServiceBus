namespace NServiceBus.Licensing
{
    partial class LicenseExpiredForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LicenseExpiredForm));
            this.headerPanel = new System.Windows.Forms.Panel();
            this.logoPictureBox = new System.Windows.Forms.PictureBox();
            this.thanksLabel = new System.Windows.Forms.Label();
            this.browseButton = new System.Windows.Forms.Button();
            this.purchaseButton = new System.Windows.Forms.Button();
            this.warningText = new System.Windows.Forms.RichTextBox();
            this.instructionsText = new System.Windows.Forms.RichTextBox();
            this.getTrialLicenseButton = new System.Windows.Forms.Button();
            this.headerPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.logoPictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // headerPanel
            // 
            this.headerPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.headerPanel.BackColor = System.Drawing.Color.Black;
            this.headerPanel.Controls.Add(this.logoPictureBox);
            this.headerPanel.Location = new System.Drawing.Point(0, 0);
            this.headerPanel.Name = "headerPanel";
            this.headerPanel.Size = new System.Drawing.Size(579, 90);
            this.headerPanel.TabIndex = 0;
            // 
            // logoPictureBox
            // 
            this.logoPictureBox.Image = global::NServiceBus.Properties.Resources.logo;
            this.logoPictureBox.InitialImage = null;
            this.logoPictureBox.Location = new System.Drawing.Point(43, 12);
            this.logoPictureBox.Name = "logoPictureBox";
            this.logoPictureBox.Size = new System.Drawing.Size(286, 65);
            this.logoPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.logoPictureBox.TabIndex = 0;
            this.logoPictureBox.TabStop = false;
            // 
            // thanksLabel
            // 
            this.thanksLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.thanksLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.thanksLabel.ForeColor = System.Drawing.Color.Black;
            this.thanksLabel.Location = new System.Drawing.Point(39, 105);
            this.thanksLabel.Name = "thanksLabel";
            this.thanksLabel.Size = new System.Drawing.Size(562, 48);
            this.thanksLabel.TabIndex = 2;
            this.thanksLabel.Text = "Thank you for using NServiceBus in Particular";
            // 
            // browseButton
            // 
            this.browseButton.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.browseButton.BackColor = System.Drawing.Color.Gainsboro;
            this.browseButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.browseButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.browseButton.Location = new System.Drawing.Point(423, 268);
            this.browseButton.Name = "browseButton";
            this.browseButton.Size = new System.Drawing.Size(140, 30);
            this.browseButton.TabIndex = 2;
            this.browseButton.Text = "Browse...";
            this.browseButton.UseVisualStyleBackColor = false;
            this.browseButton.Click += new System.EventHandler(this.browseButton_Click);
            // 
            // purchaseButton
            // 
            this.purchaseButton.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.purchaseButton.BackColor = System.Drawing.Color.Gainsboro;
            this.purchaseButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.purchaseButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.purchaseButton.Location = new System.Drawing.Point(277, 268);
            this.purchaseButton.Name = "purchaseButton";
            this.purchaseButton.Size = new System.Drawing.Size(140, 30);
            this.purchaseButton.TabIndex = 1;
            this.purchaseButton.Text = "Buy Now";
            this.purchaseButton.UseVisualStyleBackColor = false;
            this.purchaseButton.Click += new System.EventHandler(this.PurchaseButton_Click);
            // 
            // warningText
            // 
            this.warningText.BackColor = System.Drawing.Color.White;
            this.warningText.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.warningText.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.warningText.ForeColor = System.Drawing.Color.Red;
            this.warningText.Location = new System.Drawing.Point(43, 138);
            this.warningText.Name = "warningText";
            this.warningText.ReadOnly = true;
            this.warningText.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
            this.warningText.Size = new System.Drawing.Size(520, 28);
            this.warningText.TabIndex = 17;
            this.warningText.Text = "The Trial period is now over";
            // 
            // instructionsText
            // 
            this.instructionsText.BackColor = System.Drawing.Color.White;
            this.instructionsText.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.instructionsText.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.instructionsText.ForeColor = System.Drawing.Color.Black;
            this.instructionsText.Location = new System.Drawing.Point(43, 185);
            this.instructionsText.Name = "instructionsText";
            this.instructionsText.ReadOnly = true;
            this.instructionsText.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
            this.instructionsText.Size = new System.Drawing.Size(520, 77);
            this.instructionsText.TabIndex = 18;
            this.instructionsText.Text = "Instructions....";
            // 
            // getTrialLicenseButton
            // 
            this.getTrialLicenseButton.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.getTrialLicenseButton.BackColor = System.Drawing.Color.Gainsboro;
            this.getTrialLicenseButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.getTrialLicenseButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.getTrialLicenseButton.Location = new System.Drawing.Point(131, 268);
            this.getTrialLicenseButton.Name = "getTrialLicenseButton";
            this.getTrialLicenseButton.Size = new System.Drawing.Size(140, 30);
            this.getTrialLicenseButton.TabIndex = 19;
            this.getTrialLicenseButton.Text = "Get trial";
            this.getTrialLicenseButton.UseVisualStyleBackColor = false;
            this.getTrialLicenseButton.Click += new System.EventHandler(this.getTrialLicenseButton_Click);
            // 
            // LicenseExpiredForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(579, 322);
            this.Controls.Add(this.getTrialLicenseButton);
            this.Controls.Add(this.instructionsText);
            this.Controls.Add(this.warningText);
            this.Controls.Add(this.purchaseButton);
            this.Controls.Add(this.browseButton);
            this.Controls.Add(this.thanksLabel);
            this.Controls.Add(this.headerPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "LicenseExpiredForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "NServiceBus - Trial Expired";
            this.headerPanel.ResumeLayout(false);
            this.headerPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.logoPictureBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel headerPanel;
        private System.Windows.Forms.PictureBox logoPictureBox;
        private System.Windows.Forms.Label thanksLabel;
        private System.Windows.Forms.Button browseButton;
        private System.Windows.Forms.Button purchaseButton;
        private System.Windows.Forms.RichTextBox warningText;
        private System.Windows.Forms.RichTextBox instructionsText;
        private System.Windows.Forms.Button getTrialLicenseButton;
    }
}