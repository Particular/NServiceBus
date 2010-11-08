using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Grid;

namespace UI
{
    public partial class Form1 : Form
    {
        private List<ManagedEndpoint> endpoints;
        private ManagedEndpoint current;
        private Worker worker;

        public Form1()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            endpoints = Manager.GetManagedEndpoints();
            this.timer1.Start();
            this.RefreshList();
        }

        private void RefreshList()
        {
            this.ManagedEndpointList.SuspendLayout();

            object selected = this.ManagedEndpointList.SelectedItem;

            this.ManagedEndpointList.DataSource = null;
            this.ManagedEndpointList.DataSource = endpoints;

            this.ManagedEndpointList.SelectedItem = selected;

            this.ManagedEndpointList.ResumeLayout(true);

            current = this.ManagedEndpointList.SelectedItem as ManagedEndpoint;
        }

        private void ManagedEndpointButton_Click(object sender, EventArgs e)
        {
            if (ManagedEndpointList.SelectedItem != null)
                this.DoUpdate();
            else
                this.DoAdd();
        }

        private void DoAdd()
        {
            ManagedEndpoint endpoint = new ManagedEndpoint();
            endpoint.Name = ManagedEndpointName.Text;
            endpoint.Queue = ManagedEndpointQueue.Text;

            endpoints.Add(endpoint);
            
            Manager.StoreManagedEndpoints(this.endpoints);
            
            this.RefreshList();

            this.ManagedEndpointList.SelectedItem = endpoint;

            this.PrepareForUpdate();
        }

        private void DoUpdate()
        {
            current = ManagedEndpointList.SelectedItem as ManagedEndpoint;
            if (current != null)
            {
                current.Name = ManagedEndpointName.Text;
                current.Queue = ManagedEndpointQueue.Text;

                Manager.StoreManagedEndpoints(this.endpoints);

                this.RefreshList();
            }
        }

        private void ManagedEndpointList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ManagedEndpointList.SelectedItem != null)
                this.PrepareForUpdate();
            else
                this.PrepareForAdd();
        }

        private void PrepareForAdd()
        {
            this.ManagedEndpointButton.Text = "Add";
            this.ManagedEndpointName.Text = string.Empty;
            this.ManagedEndpointQueue.Text = string.Empty;

            this.MessagesInEndpoint.Text = "?";
            this.AgeOfOldestMessage.Text = "?";
            current = null;
            this.DeleteManagedEndpoint.Enabled = false;
            this.RefreshWorkerList();
        }

        private void PrepareForUpdate()
        {
            current = ManagedEndpointList.SelectedItem as ManagedEndpoint;
            if (current != null)
            {
                this.ManagedEndpointButton.Text = "Update";
                this.ManagedEndpointName.Text = current.Name;
                this.ManagedEndpointQueue.Text = current.Queue;

                this.UpdateManagedEndpoint();

                this.DeleteManagedEndpoint.Enabled = true;
                this.RefreshWorkerList();
            }
        }

        private void UpdateManagedEndpoint()
        {
            if (current != null)
            {
                this.MessagesInEndpoint.Text = current.NumberOfMessages.ToString();
                this.AgeOfOldestMessage.Text = (int)current.AgeOfOldestMessage.TotalSeconds + "s";

            }
        }

        private void ClearManagedEndpoint_Click(object sender, EventArgs e)
        {
            ManagedEndpointList.SelectedIndex = -1;
        }

        private void RefreshWorkerList()
        {
            if (current != null)
            {
                this.WorkerList.SuspendLayout();

                object selected = this.WorkerList.SelectedItem;

                this.WorkerList.DataSource = null;
                this.WorkerList.DataSource = current.Workers;

                this.WorkerList.SelectedItem = selected;

                this.WorkerList.ResumeLayout(true);

                worker = this.WorkerList.SelectedItem as Worker;
            }
            else
                WorkerList.DataSource = null;
        }

        private void WorkersList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (WorkerList.SelectedItem != null)
                this.PrepareWorkerForUpdate();
            else
                this.PrepareWorkerForAdd();
        }

        private void PrepareWorkerForAdd()
        {
            this.WorkerButton.Text = "Add";
            this.WorkerQueue.Text = string.Empty;

            this.NumberOfWorkerThreads.Text = "?";
            worker = null;

            this.DeleteWorker.Enabled = false;
        }

        private void PrepareWorkerForUpdate()
        {
            worker = WorkerList.SelectedItem as Worker;
            if (worker != null)
            {
                this.WorkerButton.Text = "Update";
                this.WorkerQueue.Text = worker.Queue;
                this.NumberOfWorkerThreads.Text = worker.NumberOfWorkerThreads.ToString();

                this.DeleteWorker.Enabled = true;
            }
        }

        private void UpdateWorker()
        {
            if (worker != null)
                this.NumberOfWorkerThreads.Text = worker.NumberOfWorkerThreads.ToString();
        }

        private void WorkerButton_Click(object sender, EventArgs e)
        {
            if (WorkerList.SelectedItem != null)
                this.DoWorkerUpdate();
            else
                this.DoWorkerAdd();
        }

        private void ClearWorker_Click(object sender, EventArgs e)
        {
            WorkerList.SelectedIndex = -1;
        }

        private void DoWorkerAdd()
        {
            if (current == null)
                return;

            Worker w = new Worker();
            w.Queue = WorkerQueue.Text;

            current.Workers.Add(w);

            this.RefreshWorkerList();

            this.WorkerList.SelectedItem = w;

            this.PrepareForUpdate();
        }

        private void DoWorkerUpdate()
        {
            worker = WorkerList.SelectedItem as Worker;
            if (worker != null)
            {
                worker.Queue = WorkerQueue.Text;

                this.RefreshWorkerList();
            }
        }

        private void RefreshNumberOfWorkerThreads_Click(object sender, EventArgs e)
        {
            if (worker != null)
                Manager.RefreshNumberOfWorkerThreads(worker.Queue);
        }

        private void IncreaseWorkerThreads_Click(object sender, EventArgs e)
        {
            if (worker != null)
                Manager.SetNumberOfWorkerThreads(worker.Queue, worker.NumberOfWorkerThreads + 1);
        }

        private void DecreaseWorkerThreads_Click(object sender, EventArgs e)
        {
            if (worker != null)
                Manager.SetNumberOfWorkerThreads(worker.Queue, worker.NumberOfWorkerThreads - 1);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Manager.Save();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Manager.Save();
        }

        private void DeleteWorker_Click(object sender, EventArgs e)
        {
            if (worker != null)
            {
                current.Workers.Remove(worker);
                worker = null;

                this.RefreshWorkerList();
            }
        }

        private void DeleteManagedEndpoint_Click(object sender, EventArgs e)
        {
            if (current != null)
            {
                this.endpoints.Remove(current);
                current = null;

                this.RefreshList();
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            this.UpdateManagedEndpoint();
            this.UpdateWorker();
        }
    }
}