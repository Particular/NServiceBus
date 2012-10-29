namespace NServiceBus.Forms
{
    partial class PrepareMachine
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
            this.moreInfoLink = new System.Windows.Forms.LinkLabel();
            this.label1 = new System.Windows.Forms.Label();
            this.prepareMachineButton = new System.Windows.Forms.Button();
            this.dontBotherMeAgainButton = new System.Windows.Forms.Button();
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
            // moreInfoLink
            // 
            this.moreInfoLink.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.moreInfoLink.AutoSize = true;
            this.moreInfoLink.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.moreInfoLink.ForeColor = System.Drawing.Color.White;
            this.moreInfoLink.LinkColor = System.Drawing.Color.Blue;
            this.moreInfoLink.Location = new System.Drawing.Point(57, 229);
            this.moreInfoLink.Name = "moreInfoLink";
            this.moreInfoLink.Size = new System.Drawing.Size(264, 20);
            this.moreInfoLink.TabIndex = 1;
            this.moreInfoLink.TabStop = true;
            this.moreInfoLink.Text = "I need more info before I can decide";
            this.moreInfoLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.requestLink_LinkClicked);
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(27, 93);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(348, 60);
            this.label1.TabIndex = 2;
            this.label1.Text = "It looks like this machine isn\'t setup to run NServiceBus";
            // 
            // prepareMachineButton
            // 
            this.prepareMachineButton.BackColor = System.Drawing.Color.White;
            this.prepareMachineButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.prepareMachineButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.prepareMachineButton.Location = new System.Drawing.Point(31, 146);
            this.prepareMachineButton.Name = "prepareMachineButton";
            this.prepareMachineButton.Size = new System.Drawing.Size(346, 31);
            this.prepareMachineButton.TabIndex = 4;
            this.prepareMachineButton.Text = "Let NServiceBus prepare my machine";
            this.prepareMachineButton.UseVisualStyleBackColor = false;
            this.prepareMachineButton.Click += new System.EventHandler(this.prepareMachineButton_Click);
            // 
            // dontBotherMeAgainButton
            // 
            this.dontBotherMeAgainButton.BackColor = System.Drawing.Color.White;
            this.dontBotherMeAgainButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.dontBotherMeAgainButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dontBotherMeAgainButton.Location = new System.Drawing.Point(31, 183);
            this.dontBotherMeAgainButton.Name = "dontBotherMeAgainButton";
            this.dontBotherMeAgainButton.Size = new System.Drawing.Size(346, 31);
            this.dontBotherMeAgainButton.TabIndex = 5;
            this.dontBotherMeAgainButton.Text = "Don\'t bother me again";
            this.dontBotherMeAgainButton.UseVisualStyleBackColor = false;
            this.dontBotherMeAgainButton.Click += new System.EventHandler(this.dontBotherMeAgainButton_Click);
            // 
            // PrepareMachine
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Gray;
            this.ClientSize = new System.Drawing.Size(406, 258);
            this.Controls.Add(this.dontBotherMeAgainButton);
            this.Controls.Add(this.prepareMachineButton);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.moreInfoLink);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = global::NServiceBus.Forms.Properties.Resources.form_icon;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PrepareMachine";
            this.Text = "NServiceBus - You machine needs to be prepared";
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
        private System.Windows.Forms.LinkLabel moreInfoLink;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button prepareMachineButton;
        private System.Windows.Forms.Button dontBotherMeAgainButton;
    }
}