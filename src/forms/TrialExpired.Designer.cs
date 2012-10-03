namespace NServiceBus.Forms
{
    partial class TrialExpired
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.requestLink = new System.Windows.Forms.LinkLabel();
            this.label1 = new System.Windows.Forms.Label();
            this.currentLicenseExpiredDateLabel = new System.Windows.Forms.Label();
            this.browseButton = new System.Windows.Forms.Button();
            this.errorMessageLabel = new System.Windows.Forms.Label();
            this.selectedFileExpirationDataLabel = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.White;
            this.panel1.Controls.Add(this.pictureBox1);
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(404, 90);
            this.panel1.TabIndex = 0;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::NServiceBus.Forms.Properties.Resources.logo;
            this.pictureBox1.InitialImage = global::NServiceBus.Forms.Properties.Resources.logo;
            this.pictureBox1.Location = new System.Drawing.Point(36, 5);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(330, 77);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // requestLink
            // 
            this.requestLink.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.requestLink.AutoSize = true;
            this.requestLink.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.requestLink.ForeColor = System.Drawing.Color.White;
            this.requestLink.LinkColor = System.Drawing.Color.Blue;
            this.requestLink.Location = new System.Drawing.Point(40, 228);
            this.requestLink.Name = "requestLink";
            this.requestLink.Size = new System.Drawing.Size(322, 20);
            this.requestLink.TabIndex = 1;
            this.requestLink.TabStop = true;
            this.requestLink.Text = "Request a free license or purchase a license";
            this.requestLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.requestLink_LinkClicked);
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(27, 122);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(348, 60);
            this.label1.TabIndex = 2;
            this.label1.Text = "The license has expired, you can continue working with an expired license or add " +
    "a valid license.";
            // 
            // currentLicenseExpiredDateLabel
            // 
            this.currentLicenseExpiredDateLabel.AutoSize = true;
            this.currentLicenseExpiredDateLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.currentLicenseExpiredDateLabel.ForeColor = System.Drawing.Color.White;
            this.currentLicenseExpiredDateLabel.Location = new System.Drawing.Point(27, 97);
            this.currentLicenseExpiredDateLabel.Name = "currentLicenseExpiredDateLabel";
            this.currentLicenseExpiredDateLabel.Size = new System.Drawing.Size(233, 20);
            this.currentLicenseExpiredDateLabel.TabIndex = 3;
            this.currentLicenseExpiredDateLabel.Text = "Expiration Date: 21/10/2010";
            // 
            // browseButton
            // 
            this.browseButton.BackColor = System.Drawing.Color.White;
            this.browseButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.browseButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.browseButton.Location = new System.Drawing.Point(98, 189);
            this.browseButton.Name = "browseButton";
            this.browseButton.Size = new System.Drawing.Size(207, 31);
            this.browseButton.TabIndex = 4;
            this.browseButton.Text = "Browse for License File";
            this.browseButton.UseVisualStyleBackColor = false;
            this.browseButton.Click += new System.EventHandler(this.browseButton_Click);
            // 
            // errorMessageLabel
            // 
            this.errorMessageLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.errorMessageLabel.ForeColor = System.Drawing.Color.White;
            this.errorMessageLabel.Location = new System.Drawing.Point(27, 224);
            this.errorMessageLabel.Name = "errorMessageLabel";
            this.errorMessageLabel.Size = new System.Drawing.Size(348, 24);
            this.errorMessageLabel.TabIndex = 5;
            this.errorMessageLabel.Text = "The file you have selected is not valid.";
            this.errorMessageLabel.Visible = false;
            // 
            // selectedFileExpirationDataLabel
            // 
            this.selectedFileExpirationDataLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.selectedFileExpirationDataLabel.ForeColor = System.Drawing.Color.White;
            this.selectedFileExpirationDataLabel.Location = new System.Drawing.Point(27, 248);
            this.selectedFileExpirationDataLabel.Name = "selectedFileExpirationDataLabel";
            this.selectedFileExpirationDataLabel.Size = new System.Drawing.Size(348, 24);
            this.selectedFileExpirationDataLabel.TabIndex = 6;
            this.selectedFileExpirationDataLabel.Text = "Expiration Date: ";
            this.selectedFileExpirationDataLabel.Visible = false;
            // 
            // TrialExpired
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Gray;
            this.ClientSize = new System.Drawing.Size(406, 258);
            this.Controls.Add(this.browseButton);
            this.Controls.Add(this.currentLicenseExpiredDateLabel);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.requestLink);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.selectedFileExpirationDataLabel);
            this.Controls.Add(this.errorMessageLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = global::NServiceBus.Forms.Properties.Resources.form_icon;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "TrialExpired";
            this.Text = "NServiceBus - Trial Expired";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.TrialExpired_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.LinkLabel requestLink;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label currentLicenseExpiredDateLabel;
        private System.Windows.Forms.Button browseButton;
        private System.Windows.Forms.Label errorMessageLabel;
        private System.Windows.Forms.Label selectedFileExpirationDataLabel;
    }
}