namespace NServiceBus.Forms
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Windows.Forms;

    public partial class TrialExpired : Form
    {
        public TrialExpired()
        {
            InitializeComponent();
        }

        public Func<TrialExpired, string, bool> ValidateLicenseFile { get; set; }

        public DateTime CurrentLicenseExpireDate { get; set; }

        public void DisplayError()
        {
            errorPanel.Visible = true;
            errorMessageLabel.Text = "The file you have selected is not valid.";
            selectedFileExpirationDateLabel.Text = String.Empty;
            Height = 376;
        }

        public void DisplayExpiredLicenseError(DateTime expirationDate)
        {
            errorPanel.Visible = true;
            errorMessageLabel.Text = "The license file you have selected is expired.";
            selectedFileExpirationDateLabel.Text = String.Format("Expiration Date: {0}", expirationDate.ToLocalTime().ToShortDateString());
            Height = 376;
        }

        private void browseButton_Click(object sender, EventArgs e)
        {
            var close = false;

            using (var openDialog = new OpenFileDialog())
            {
                openDialog.InitializeLifetimeService();
                openDialog.Filter = "License files (*.xml)|*.xml|All files (*.*)|*.*";
                openDialog.Title = "Select License file";

                if (ShowDialogInSTA(openDialog) == DialogResult.OK)
                {
                    string licenseFileSelected = openDialog.FileName;

                    if (ValidateLicenseFile(this, licenseFileSelected))
                    {
                        close = true;
                    }
                }
            }

            if (close)
            {
                browseButton.Enabled = false;
                errorPanel.Visible = false;
                completePanel.Visible = true;
                Height = 376;

                timer.Tick += delegate
                    {
                        timer.Enabled = false;
                        DialogResult = DialogResult.OK;
                    };

                timer.Interval = 5000;
                timer.Enabled = true;
            }
        }

        //When calling a OpenFileDialog it needs to be done from an STA thread model otherwise it throws:
        //"Current thread must be set to single thread apartment (STA) mode before OLE calls can be made. Ensure that your Main function has STAThreadAttribute marked on it. This exception is only raised if a debugger is attached to the process."
        private static DialogResult ShowDialogInSTA(FileDialog dialog)
        {
            var result = DialogResult.Cancel;
            
            var thread = new Thread(() =>
                {
                    result = dialog.ShowDialog();
                });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            return result;
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            Activate();
            timer.Enabled = false;

            timer.Tick -= timer_Tick;
        }

        private void requestButton_Click(object sender, EventArgs e)
        {
            Process.Start("http://particular.net/licensing");
        }

        private void ignoreButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }
    }
}
