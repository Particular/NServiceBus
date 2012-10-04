namespace NServiceBus.Forms
{
    partial class FirstTimeUser
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
            this.label1 = new System.Windows.Forms.Label();
            this.openGettingStartedButton = new System.Windows.Forms.Button();
            this.noThanksButtton = new System.Windows.Forms.Button();
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
            this.panel1.Size = new System.Drawing.Size(439, 90);
            this.panel1.TabIndex = 0;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::NServiceBus.Forms.Properties.Resources.logo;
            this.pictureBox1.InitialImage = global::NServiceBus.Forms.Properties.Resources.logo;
            this.pictureBox1.Location = new System.Drawing.Point(12, 3);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(330, 77);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(12, 103);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(394, 60);
            this.label1.TabIndex = 2;
            this.label1.Text = "First time using NServiceBus?";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // openGettingStartedButton
            // 
            this.openGettingStartedButton.BackColor = System.Drawing.Color.White;
            this.openGettingStartedButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.openGettingStartedButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.openGettingStartedButton.Location = new System.Drawing.Point(12, 166);
            this.openGettingStartedButton.Name = "openGettingStartedButton";
            this.openGettingStartedButton.Size = new System.Drawing.Size(394, 31);
            this.openGettingStartedButton.TabIndex = 4;
            this.openGettingStartedButton.Text = "Take me to the Getting Started guide";
            this.openGettingStartedButton.UseVisualStyleBackColor = false;
            this.openGettingStartedButton.Click += new System.EventHandler(this.openGettingStartedButton_Click);
            // 
            // noThanksButtton
            // 
            this.noThanksButtton.BackColor = System.Drawing.Color.White;
            this.noThanksButtton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.noThanksButtton.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.noThanksButtton.Location = new System.Drawing.Point(12, 203);
            this.noThanksButtton.Name = "noThanksButtton";
            this.noThanksButtton.Size = new System.Drawing.Size(394, 31);
            this.noThanksButtton.TabIndex = 5;
            this.noThanksButtton.Text = "No thanks, I know my way around NServiceBus";
            this.noThanksButtton.UseVisualStyleBackColor = false;
            this.noThanksButtton.Click += new System.EventHandler(this.dontBotherMeAgainButton_Click);
            // 
            // FirstTimeUser
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Gray;
            this.ClientSize = new System.Drawing.Size(422, 244);
            this.Controls.Add(this.noThanksButtton);
            this.Controls.Add(this.openGettingStartedButton);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = global::NServiceBus.Forms.Properties.Resources.form_icon;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FirstTimeUser";
            this.Text = "NServiceBus - First time user?";
            this.TopMost = true;
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button openGettingStartedButton;
        private System.Windows.Forms.Button noThanksButtton;
    }
}