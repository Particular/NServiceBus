namespace Customer
{
    partial class CustomerOrder
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
            this.OrderButton = new System.Windows.Forms.Button();
            this.NameLabel = new System.Windows.Forms.Label();
            this.NameTextBox = new System.Windows.Forms.TextBox();
            this.DrinkLabel = new System.Windows.Forms.Label();
            this.SizeLabel = new System.Windows.Forms.Label();
            this.DrinkComboBox = new System.Windows.Forms.ComboBox();
            this.SizeComboBox = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // OrderButton
            // 
            this.OrderButton.Location = new System.Drawing.Point(127, 164);
            this.OrderButton.Name = "OrderButton";
            this.OrderButton.Size = new System.Drawing.Size(75, 23);
            this.OrderButton.TabIndex = 3;
            this.OrderButton.Text = "Order";
            this.OrderButton.UseVisualStyleBackColor = true;
            this.OrderButton.Click += new System.EventHandler(this.OrderButton_Click);
            // 
            // NameLabel
            // 
            this.NameLabel.AutoSize = true;
            this.NameLabel.Location = new System.Drawing.Point(12, 9);
            this.NameLabel.Name = "NameLabel";
            this.NameLabel.Size = new System.Drawing.Size(58, 13);
            this.NameLabel.TabIndex = 1;
            this.NameLabel.Text = "Your name";
            // 
            // NameTextBox
            // 
            this.NameTextBox.Location = new System.Drawing.Point(15, 25);
            this.NameTextBox.Name = "NameTextBox";
            this.NameTextBox.Size = new System.Drawing.Size(187, 20);
            this.NameTextBox.TabIndex = 0;
            // 
            // DrinkLabel
            // 
            this.DrinkLabel.AutoSize = true;
            this.DrinkLabel.Location = new System.Drawing.Point(12, 59);
            this.DrinkLabel.Name = "DrinkLabel";
            this.DrinkLabel.Size = new System.Drawing.Size(32, 13);
            this.DrinkLabel.TabIndex = 3;
            this.DrinkLabel.Text = "Drink";
            // 
            // SizeLabel
            // 
            this.SizeLabel.AutoSize = true;
            this.SizeLabel.Location = new System.Drawing.Point(12, 109);
            this.SizeLabel.Name = "SizeLabel";
            this.SizeLabel.Size = new System.Drawing.Size(27, 13);
            this.SizeLabel.TabIndex = 4;
            this.SizeLabel.Text = "Size";
            // 
            // DrinkComboBox
            // 
            this.DrinkComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.DrinkComboBox.FormattingEnabled = true;
            this.DrinkComboBox.Items.AddRange(new object[] {
            "Caramel Macchiato",
            "Mocha",
            "Americano",
            "Latte",
            "Cuban",
            "Frapachappaccino"});
            this.DrinkComboBox.Location = new System.Drawing.Point(15, 75);
            this.DrinkComboBox.Name = "DrinkComboBox";
            this.DrinkComboBox.Size = new System.Drawing.Size(187, 21);
            this.DrinkComboBox.TabIndex = 1;
            // 
            // SizeComboBox
            // 
            this.SizeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.SizeComboBox.FormattingEnabled = true;
            this.SizeComboBox.Items.AddRange(new object[] {
            "Tall",
            "Grande",
            "Venti"});
            this.SizeComboBox.Location = new System.Drawing.Point(15, 125);
            this.SizeComboBox.Name = "SizeComboBox";
            this.SizeComboBox.Size = new System.Drawing.Size(187, 21);
            this.SizeComboBox.TabIndex = 2;
            // 
            // CustomerOrder
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(217, 196);
            this.Controls.Add(this.SizeComboBox);
            this.Controls.Add(this.DrinkComboBox);
            this.Controls.Add(this.SizeLabel);
            this.Controls.Add(this.DrinkLabel);
            this.Controls.Add(this.NameTextBox);
            this.Controls.Add(this.NameLabel);
            this.Controls.Add(this.OrderButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "CustomerOrder";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Order @ Starbucks";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button OrderButton;
        private System.Windows.Forms.Label NameLabel;
        private System.Windows.Forms.TextBox NameTextBox;
        private System.Windows.Forms.Label DrinkLabel;
        private System.Windows.Forms.Label SizeLabel;
        private System.Windows.Forms.ComboBox DrinkComboBox;
        private System.Windows.Forms.ComboBox SizeComboBox;

    }
}
