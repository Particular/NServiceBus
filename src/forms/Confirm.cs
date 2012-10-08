namespace NServiceBus.Forms
{
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.Windows.Forms;

    public partial class Confirm : Form
    {
        public Confirm()
        {
            InitializeComponent();
        }

        public string ConfirmText{ get; set; }

        public bool Ok { get; set; }
        public bool OkOnly { get; set; }

        public bool Cancel 
        { 
            get { return !Ok; }
        }


        private void Confirm_Load(object sender, EventArgs e)
        {
            label1.Text = ConfirmText;

            if(OkOnly)
            {
                cancelButton.Visible = !OkOnly;
                okButton.Location = new Point(135,215);
            }
            
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            Ok = true;
            Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

 


       
    }
}
