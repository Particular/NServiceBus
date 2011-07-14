namespace TimeoutManager
{
    partial class StarbucksTimeoutManager
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
            if(disposing && (components != null))
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
            this.TimoutsListBox = new System.Windows.Forms.ListBox();
            this.TimeoutsLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // TimoutsListBox
            // 
            this.TimoutsListBox.FormattingEnabled = true;
            this.TimoutsListBox.Location = new System.Drawing.Point(12, 25);
            this.TimoutsListBox.Name = "TimoutsListBox";
            this.TimoutsListBox.Size = new System.Drawing.Size(557, 290);
            this.TimoutsListBox.TabIndex = 0;
            // 
            // TimeoutsLabel
            // 
            this.TimeoutsLabel.AutoSize = true;
            this.TimeoutsLabel.Location = new System.Drawing.Point(12, 9);
            this.TimeoutsLabel.Name = "TimeoutsLabel";
            this.TimeoutsLabel.Size = new System.Drawing.Size(50, 13);
            this.TimeoutsLabel.TabIndex = 1;
            this.TimeoutsLabel.Text = "Timeouts";
            // 
            // StarbucksTimeoutManager
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(582, 330);
            this.Controls.Add(this.TimeoutsLabel);
            this.Controls.Add(this.TimoutsListBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "StarbucksTimeoutManager";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Timout Monitor 2000 - Ultimate Edition";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox TimoutsListBox;
        private System.Windows.Forms.Label TimeoutsLabel;
    }
}

