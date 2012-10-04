namespace NServiceBus.Forms
{
    using System;
    using System.Windows.Forms;

    public partial class FirstTimeUser : Form
    {
        public FirstTimeUser()
        {
            InitializeComponent();
        }

        public bool OpenGettingStartedGuide{ get; set; }

        public bool DontBotherMeAgain { get; set; }

        private void dontBotherMeAgainButton_Click(object sender, EventArgs e)
        {
            DontBotherMeAgain = true;

            Close();
        }

        private void openGettingStartedButton_Click(object sender, EventArgs e)
        {
            DontBotherMeAgain = true;
            OpenGettingStartedGuide = true;

            Close();
      
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
