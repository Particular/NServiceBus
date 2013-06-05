namespace NServiceBus.Forms
{
    using System;
    using System.Diagnostics;
    using System.Windows.Forms;

    public partial class PrepareMachine : Form
    {
        public PrepareMachine()
        {
            InitializeComponent();
        }

        public bool AllowPrepare{ get; set; }

        public bool DontBotherMeAgain { get; set; }


        private void requestLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://particular.net/articles/running-nservicebus-on-windows");
        }


   

        private void prepareMachineButton_Click(object sender, EventArgs e)
        {
            AllowPrepare = true;

            Close();
        }

        private void dontBotherMeAgainButton_Click(object sender, EventArgs e)
        {
            DontBotherMeAgain = true;

            Close();
        }
    }
}
