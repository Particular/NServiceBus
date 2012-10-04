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
            this.components = new System.ComponentModel.Container();
            this.panel1 = new System.Windows.Forms.Panel();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.requestLink = new System.Windows.Forms.LinkLabel();
            this.label1 = new System.Windows.Forms.Label();
            this.browseButton = new System.Windows.Forms.Button();
            this.errorMessageLabel = new System.Windows.Forms.Label();
            this.selectedFileExpirationDateLabel = new System.Windows.Forms.Label();
            this.ignoreLink = new System.Windows.Forms.LinkLabel();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.notTopMostTimer = new System.Windows.Forms.Timer(this.components);
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.BackColor = System.Drawing.Color.White;
            this.panel1.Controls.Add(this.pictureBox1);
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(394, 90);
            this.panel1.TabIndex = 0;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::NServiceBus.Forms.Properties.Resources.logo;
            this.pictureBox1.InitialImage = global::NServiceBus.Forms.Properties.Resources.logo;
            this.pictureBox1.Location = new System.Drawing.Point(32, 5);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(330, 77);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // requestLink
            // 
            this.requestLink.AutoSize = true;
            this.requestLink.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.requestLink.ForeColor = System.Drawing.Color.White;
            this.requestLink.LinkColor = System.Drawing.Color.Blue;
            this.requestLink.Location = new System.Drawing.Point(7, 172);
            this.requestLink.Name = "requestLink";
            this.requestLink.Size = new System.Drawing.Size(134, 20);
            this.requestLink.TabIndex = 1;
            this.requestLink.TabStop = true;
            this.requestLink.Text = "Get a free license";
            this.requestLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.requestLink_LinkClicked);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(7, 97);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(377, 64);
            this.label1.TabIndex = 2;
            this.label1.Text = "Unfortunately your license has expired. But, you can get a free license that will" +
    " allow you to continue working.\r\n";
            // 
            // browseButton
            // 
            this.browseButton.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.browseButton.BackColor = System.Drawing.Color.White;
            this.browseButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.browseButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.browseButton.Location = new System.Drawing.Point(147, 272);
            this.browseButton.Name = "browseButton";
            this.browseButton.Size = new System.Drawing.Size(101, 31);
            this.browseButton.TabIndex = 4;
            this.browseButton.Text = "Browse...";
            this.browseButton.UseVisualStyleBackColor = false;
            this.browseButton.Click += new System.EventHandler(this.browseButton_Click);
            // 
            // errorMessageLabel
            // 
            this.errorMessageLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.errorMessageLabel.ForeColor = System.Drawing.Color.White;
            this.errorMessageLabel.Location = new System.Drawing.Point(9, 308);
            this.errorMessageLabel.Name = "errorMessageLabel";
            this.errorMessageLabel.Size = new System.Drawing.Size(348, 24);
            this.errorMessageLabel.TabIndex = 5;
            this.errorMessageLabel.Text = "The file you have selected is not valid.";
            this.errorMessageLabel.Visible = false;
            // 
            // selectedFileExpirationDateLabel
            // 
            this.selectedFileExpirationDateLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.selectedFileExpirationDateLabel.ForeColor = System.Drawing.Color.White;
            this.selectedFileExpirationDateLabel.Location = new System.Drawing.Point(9, 332);
            this.selectedFileExpirationDateLabel.Name = "selectedFileExpirationDateLabel";
            this.selectedFileExpirationDateLabel.Size = new System.Drawing.Size(348, 24);
            this.selectedFileExpirationDateLabel.TabIndex = 6;
            this.selectedFileExpirationDateLabel.Text = "Expiration Date: ";
            this.selectedFileExpirationDateLabel.Visible = false;
            // 
            // ignoreLink
            // 
            this.ignoreLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ignoreLink.AutoSize = true;
            this.ignoreLink.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ignoreLink.ForeColor = System.Drawing.Color.White;
            this.ignoreLink.LinkColor = System.Drawing.Color.Blue;
            this.ignoreLink.Location = new System.Drawing.Point(321, 172);
            this.ignoreLink.Name = "ignoreLink";
            this.ignoreLink.Size = new System.Drawing.Size(55, 20);
            this.ignoreLink.TabIndex = 7;
            this.ignoreLink.TabStop = true;
            this.ignoreLink.Text = "Ignore";
            this.ignoreLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.ignoreLink_LinkClicked);
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.White;
            this.label2.Location = new System.Drawing.Point(7, 203);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(377, 33);
            this.label2.TabIndex = 8;
            this.label2.Text = "This warning will continue to appear until a valid license is provided.";
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.Color.White;
            this.label3.Location = new System.Drawing.Point(7, 245);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(379, 24);
            this.label3.TabIndex = 9;
            this.label3.Text = "Once you have received your license, enter it here:";
            // 
            // notTopMostTimer
            // 
            this.notTopMostTimer.Enabled = true;
            this.notTopMostTimer.Interval = 3000;
            this.notTopMostTimer.Tick += new System.EventHandler(this.notTopMostTimer_Tick);
            // 
            // TrialExpired
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Gray;
            this.ClientSize = new System.Drawing.Size(394, 310);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.ignoreLink);
            this.Controls.Add(this.browseButton);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.requestLink);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.selectedFileExpirationDateLabel);
            this.Controls.Add(this.errorMessageLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = global::NServiceBus.Forms.Properties.Resources.form_icon;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "TrialExpired";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "NServiceBus - Trial Expired";
            this.TopMost = true;
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
        private System.Windows.Forms.Button browseButton;
        private System.Windows.Forms.Label errorMessageLabel;
        private System.Windows.Forms.Label selectedFileExpirationDateLabel;
        private System.Windows.Forms.LinkLabel ignoreLink;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Timer notTopMostTimer;
    }
}