namespace UI
{
    partial class Form1
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
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.ManagedEndpointList = new System.Windows.Forms.ListBox();
            this.DeleteManagedEndpoint = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.MessagesInEndpoint = new System.Windows.Forms.Label();
            this.ClearManagedEndpoint = new System.Windows.Forms.Button();
            this.ManagedEndpointButton = new System.Windows.Forms.Button();
            this.ManagedEndpointQueue = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.ManagedEndpointName = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.splitContainer3 = new System.Windows.Forms.SplitContainer();
            this.WorkerList = new System.Windows.Forms.ListBox();
            this.DeleteWorker = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.DecreaseWorkerThreads = new System.Windows.Forms.Button();
            this.IncreaseWorkerThreads = new System.Windows.Forms.Button();
            this.RefreshNumberOfWorkerThreads = new System.Windows.Forms.Button();
            this.NumberOfWorkerThreads = new System.Windows.Forms.Label();
            this.ClearWorker = new System.Windows.Forms.Button();
            this.WorkerButton = new System.Windows.Forms.Button();
            this.WorkerQueue = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.AgeOfOldestMessage = new System.Windows.Forms.Label();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.splitContainer3.Panel1.SuspendLayout();
            this.splitContainer3.Panel2.SuspendLayout();
            this.splitContainer3.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 24);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.splitContainer2);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer3);
            this.splitContainer1.Size = new System.Drawing.Size(559, 408);
            this.splitContainer1.SplitterDistance = 254;
            this.splitContainer1.TabIndex = 0;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.ManagedEndpointList);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.groupBox3);
            this.splitContainer2.Panel2.Controls.Add(this.DeleteManagedEndpoint);
            this.splitContainer2.Panel2.Controls.Add(this.groupBox1);
            this.splitContainer2.Panel2.Controls.Add(this.ClearManagedEndpoint);
            this.splitContainer2.Panel2.Controls.Add(this.ManagedEndpointButton);
            this.splitContainer2.Panel2.Controls.Add(this.ManagedEndpointQueue);
            this.splitContainer2.Panel2.Controls.Add(this.label3);
            this.splitContainer2.Panel2.Controls.Add(this.ManagedEndpointName);
            this.splitContainer2.Panel2.Controls.Add(this.label2);
            this.splitContainer2.Panel2.Controls.Add(this.label1);
            this.splitContainer2.Size = new System.Drawing.Size(254, 408);
            this.splitContainer2.SplitterDistance = 198;
            this.splitContainer2.TabIndex = 0;
            // 
            // ManagedEndpointList
            // 
            this.ManagedEndpointList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ManagedEndpointList.FormattingEnabled = true;
            this.ManagedEndpointList.Location = new System.Drawing.Point(0, 0);
            this.ManagedEndpointList.Name = "ManagedEndpointList";
            this.ManagedEndpointList.Size = new System.Drawing.Size(254, 186);
            this.ManagedEndpointList.TabIndex = 0;
            this.ManagedEndpointList.SelectedIndexChanged += new System.EventHandler(this.ManagedEndpointList_SelectedIndexChanged);
            // 
            // DeleteManagedEndpoint
            // 
            this.DeleteManagedEndpoint.Location = new System.Drawing.Point(99, 94);
            this.DeleteManagedEndpoint.Name = "DeleteManagedEndpoint";
            this.DeleteManagedEndpoint.Size = new System.Drawing.Size(61, 23);
            this.DeleteManagedEndpoint.TabIndex = 14;
            this.DeleteManagedEndpoint.Text = "Delete";
            this.DeleteManagedEndpoint.UseVisualStyleBackColor = true;
            this.DeleteManagedEndpoint.Click += new System.EventHandler(this.DeleteManagedEndpoint_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.MessagesInEndpoint);
            this.groupBox1.Location = new System.Drawing.Point(16, 133);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(119, 73);
            this.groupBox1.TabIndex = 7;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Messages in queue";
            // 
            // MessagesInEndpoint
            // 
            this.MessagesInEndpoint.AutoSize = true;
            this.MessagesInEndpoint.Font = new System.Drawing.Font("Arial", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MessagesInEndpoint.Location = new System.Drawing.Point(24, 31);
            this.MessagesInEndpoint.Name = "MessagesInEndpoint";
            this.MessagesInEndpoint.Size = new System.Drawing.Size(23, 24);
            this.MessagesInEndpoint.TabIndex = 1;
            this.MessagesInEndpoint.Text = "?";
            // 
            // ClearManagedEndpoint
            // 
            this.ClearManagedEndpoint.Location = new System.Drawing.Point(16, 94);
            this.ClearManagedEndpoint.Name = "ClearManagedEndpoint";
            this.ClearManagedEndpoint.Size = new System.Drawing.Size(54, 23);
            this.ClearManagedEndpoint.TabIndex = 6;
            this.ClearManagedEndpoint.Text = "Clear";
            this.ClearManagedEndpoint.UseVisualStyleBackColor = true;
            this.ClearManagedEndpoint.Click += new System.EventHandler(this.ClearManagedEndpoint_Click);
            // 
            // ManagedEndpointButton
            // 
            this.ManagedEndpointButton.Location = new System.Drawing.Point(185, 94);
            this.ManagedEndpointButton.Name = "ManagedEndpointButton";
            this.ManagedEndpointButton.Size = new System.Drawing.Size(55, 23);
            this.ManagedEndpointButton.TabIndex = 5;
            this.ManagedEndpointButton.Text = "Add";
            this.ManagedEndpointButton.UseVisualStyleBackColor = true;
            this.ManagedEndpointButton.Click += new System.EventHandler(this.ManagedEndpointButton_Click);
            // 
            // ManagedEndpointQueue
            // 
            this.ManagedEndpointQueue.Location = new System.Drawing.Point(141, 67);
            this.ManagedEndpointQueue.Name = "ManagedEndpointQueue";
            this.ManagedEndpointQueue.Size = new System.Drawing.Size(100, 20);
            this.ManagedEndpointQueue.TabIndex = 4;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(96, 70);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(39, 13);
            this.label3.TabIndex = 3;
            this.label3.Text = "Queue";
            // 
            // ManagedEndpointName
            // 
            this.ManagedEndpointName.Location = new System.Drawing.Point(141, 39);
            this.ManagedEndpointName.Name = "ManagedEndpointName";
            this.ManagedEndpointName.Size = new System.Drawing.Size(100, 20);
            this.ManagedEndpointName.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(97, 42);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(38, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Name:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 14);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(97, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Managed Endpoint";
            // 
            // splitContainer3
            // 
            this.splitContainer3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer3.Location = new System.Drawing.Point(0, 0);
            this.splitContainer3.Name = "splitContainer3";
            this.splitContainer3.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer3.Panel1
            // 
            this.splitContainer3.Panel1.Controls.Add(this.WorkerList);
            // 
            // splitContainer3.Panel2
            // 
            this.splitContainer3.Panel2.Controls.Add(this.DeleteWorker);
            this.splitContainer3.Panel2.Controls.Add(this.groupBox2);
            this.splitContainer3.Panel2.Controls.Add(this.ClearWorker);
            this.splitContainer3.Panel2.Controls.Add(this.WorkerButton);
            this.splitContainer3.Panel2.Controls.Add(this.WorkerQueue);
            this.splitContainer3.Panel2.Controls.Add(this.label4);
            this.splitContainer3.Panel2.Controls.Add(this.label5);
            this.splitContainer3.Size = new System.Drawing.Size(301, 408);
            this.splitContainer3.SplitterDistance = 221;
            this.splitContainer3.TabIndex = 0;
            // 
            // WorkerList
            // 
            this.WorkerList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.WorkerList.FormattingEnabled = true;
            this.WorkerList.Location = new System.Drawing.Point(0, 0);
            this.WorkerList.Name = "WorkerList";
            this.WorkerList.Size = new System.Drawing.Size(301, 212);
            this.WorkerList.TabIndex = 0;
            this.WorkerList.SelectedIndexChanged += new System.EventHandler(this.WorkersList_SelectedIndexChanged);
            // 
            // DeleteWorker
            // 
            this.DeleteWorker.Location = new System.Drawing.Point(125, 71);
            this.DeleteWorker.Name = "DeleteWorker";
            this.DeleteWorker.Size = new System.Drawing.Size(61, 23);
            this.DeleteWorker.TabIndex = 13;
            this.DeleteWorker.Text = "Delete";
            this.DeleteWorker.UseVisualStyleBackColor = true;
            this.DeleteWorker.Click += new System.EventHandler(this.DeleteWorker_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.DecreaseWorkerThreads);
            this.groupBox2.Controls.Add(this.IncreaseWorkerThreads);
            this.groupBox2.Controls.Add(this.RefreshNumberOfWorkerThreads);
            this.groupBox2.Controls.Add(this.NumberOfWorkerThreads);
            this.groupBox2.Location = new System.Drawing.Point(41, 109);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(225, 73);
            this.groupBox2.TabIndex = 12;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Number of worker threads";
            // 
            // DecreaseWorkerThreads
            // 
            this.DecreaseWorkerThreads.Font = new System.Drawing.Font("Wingdings 3", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(2)));
            this.DecreaseWorkerThreads.Location = new System.Drawing.Point(195, 31);
            this.DecreaseWorkerThreads.Name = "DecreaseWorkerThreads";
            this.DecreaseWorkerThreads.Size = new System.Drawing.Size(24, 23);
            this.DecreaseWorkerThreads.TabIndex = 4;
            this.DecreaseWorkerThreads.Text = "q";
            this.DecreaseWorkerThreads.UseVisualStyleBackColor = true;
            this.DecreaseWorkerThreads.Click += new System.EventHandler(this.DecreaseWorkerThreads_Click);
            // 
            // IncreaseWorkerThreads
            // 
            this.IncreaseWorkerThreads.Font = new System.Drawing.Font("Wingdings 3", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(2)));
            this.IncreaseWorkerThreads.Location = new System.Drawing.Point(166, 31);
            this.IncreaseWorkerThreads.Name = "IncreaseWorkerThreads";
            this.IncreaseWorkerThreads.Size = new System.Drawing.Size(23, 23);
            this.IncreaseWorkerThreads.TabIndex = 3;
            this.IncreaseWorkerThreads.Text = "p";
            this.IncreaseWorkerThreads.UseVisualStyleBackColor = true;
            this.IncreaseWorkerThreads.Click += new System.EventHandler(this.IncreaseWorkerThreads_Click);
            // 
            // RefreshNumberOfWorkerThreads
            // 
            this.RefreshNumberOfWorkerThreads.Location = new System.Drawing.Point(7, 31);
            this.RefreshNumberOfWorkerThreads.Name = "RefreshNumberOfWorkerThreads";
            this.RefreshNumberOfWorkerThreads.Size = new System.Drawing.Size(75, 23);
            this.RefreshNumberOfWorkerThreads.TabIndex = 2;
            this.RefreshNumberOfWorkerThreads.Text = "Refresh";
            this.RefreshNumberOfWorkerThreads.UseVisualStyleBackColor = true;
            this.RefreshNumberOfWorkerThreads.Click += new System.EventHandler(this.RefreshNumberOfWorkerThreads_Click);
            // 
            // NumberOfWorkerThreads
            // 
            this.NumberOfWorkerThreads.AutoSize = true;
            this.NumberOfWorkerThreads.Font = new System.Drawing.Font("Arial", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.NumberOfWorkerThreads.Location = new System.Drawing.Point(122, 31);
            this.NumberOfWorkerThreads.Name = "NumberOfWorkerThreads";
            this.NumberOfWorkerThreads.Size = new System.Drawing.Size(23, 24);
            this.NumberOfWorkerThreads.TabIndex = 1;
            this.NumberOfWorkerThreads.Text = "?";
            // 
            // ClearWorker
            // 
            this.ClearWorker.Location = new System.Drawing.Point(42, 70);
            this.ClearWorker.Name = "ClearWorker";
            this.ClearWorker.Size = new System.Drawing.Size(56, 23);
            this.ClearWorker.TabIndex = 11;
            this.ClearWorker.Text = "Clear";
            this.ClearWorker.UseVisualStyleBackColor = true;
            this.ClearWorker.Click += new System.EventHandler(this.ClearWorker_Click);
            // 
            // WorkerButton
            // 
            this.WorkerButton.Location = new System.Drawing.Point(207, 70);
            this.WorkerButton.Name = "WorkerButton";
            this.WorkerButton.Size = new System.Drawing.Size(59, 23);
            this.WorkerButton.TabIndex = 10;
            this.WorkerButton.Text = "Add";
            this.WorkerButton.UseVisualStyleBackColor = true;
            this.WorkerButton.Click += new System.EventHandler(this.WorkerButton_Click);
            // 
            // WorkerQueue
            // 
            this.WorkerQueue.Location = new System.Drawing.Point(167, 43);
            this.WorkerQueue.Name = "WorkerQueue";
            this.WorkerQueue.Size = new System.Drawing.Size(100, 20);
            this.WorkerQueue.TabIndex = 9;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(122, 46);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(39, 13);
            this.label4.TabIndex = 8;
            this.label4.Text = "Queue";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(15, 15);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(42, 13);
            this.label5.TabIndex = 7;
            this.label5.Text = "Worker";
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(559, 24);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(35, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(98, 22);
            this.saveToolStripMenuItem.Text = "Save";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(98, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // timer1
            // 
            this.timer1.Interval = 1000;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.AgeOfOldestMessage);
            this.groupBox3.Location = new System.Drawing.Point(135, 133);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(119, 73);
            this.groupBox3.TabIndex = 15;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Oldest Message";
            // 
            // AgeOfOldestMessage
            // 
            this.AgeOfOldestMessage.AutoSize = true;
            this.AgeOfOldestMessage.Font = new System.Drawing.Font("Arial", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.AgeOfOldestMessage.Location = new System.Drawing.Point(24, 31);
            this.AgeOfOldestMessage.Name = "AgeOfOldestMessage";
            this.AgeOfOldestMessage.Size = new System.Drawing.Size(23, 24);
            this.AgeOfOldestMessage.TabIndex = 1;
            this.AgeOfOldestMessage.Text = "?";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(559, 432);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Form1";
            this.Text = "Form1";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            this.splitContainer2.Panel2.PerformLayout();
            this.splitContainer2.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.splitContainer3.Panel1.ResumeLayout(false);
            this.splitContainer3.Panel2.ResumeLayout(false);
            this.splitContainer3.Panel2.PerformLayout();
            this.splitContainer3.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.ListBox ManagedEndpointList;
        private System.Windows.Forms.Button ManagedEndpointButton;
        private System.Windows.Forms.TextBox ManagedEndpointQueue;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox ManagedEndpointName;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button ClearManagedEndpoint;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label MessagesInEndpoint;
        private System.Windows.Forms.SplitContainer splitContainer3;
        private System.Windows.Forms.ListBox WorkerList;
        private System.Windows.Forms.Button ClearWorker;
        private System.Windows.Forms.Button WorkerButton;
        private System.Windows.Forms.TextBox WorkerQueue;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label NumberOfWorkerThreads;
        private System.Windows.Forms.Button RefreshNumberOfWorkerThreads;
        private System.Windows.Forms.Button IncreaseWorkerThreads;
        private System.Windows.Forms.Button DecreaseWorkerThreads;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.Button DeleteManagedEndpoint;
        private System.Windows.Forms.Button DeleteWorker;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label AgeOfOldestMessage;
    }
}

