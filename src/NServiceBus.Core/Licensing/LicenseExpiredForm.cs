namespace NServiceBus.Licensing
{
    using System;
    using System.Diagnostics;
    using System.Windows.Forms;
    using Janitor;
    using Logging;
    using Particular.Licensing;

    [SkipWeaving]
    partial class LicenseExpiredForm : Form
    {
        static ILog Logger = LogManager.GetLogger<LicenseExpiredForm>();
        public LicenseExpiredForm()
        {
            InitializeComponent();
        }

        public License CurrentLicense { get; set; }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            warningText.Text = "The trial period is now over";

            if (CurrentLicense != null && CurrentLicense.IsTrialLicense)
            {
                Text = "NServiceBus - Initial Trial Expired";
                instructionsText.Text = "To extend your free trial, click 'Extend trial' and register online. When you receive your license file, save it to disk and then click the 'Browse' button below to select it.";
                getTrialLicenseButton.Text = "Extend Trial";
                purchaseButton.Visible = false;
                getTrialLicenseButton.Left = purchaseButton.Left;
            }
            else
            {
                Text = "NServiceBus - Extended Trial Expired";
                instructionsText.Text = "Please click 'Contact Sales' to request an extension to your free trial, or click 'Buy Now' to purchase a license online. When you receive your license file, save it to disk and then click the 'Browse' button below to select it.";
                getTrialLicenseButton.Text = "Contact Sales";
            }

            Visible = true;
        }

        void browseButton_Click(object sender, EventArgs e)
        {
            using (var openDialog = new OpenFileDialog())
            {
                openDialog.InitializeLifetimeService();
                openDialog.Filter = "License files (*.xml)|*.xml|All files (*.*)|*.*";
                openDialog.Title = "Select License file";

                var dialogResult = StaThreadRunner.ShowDialogInSTA(openDialog.ShowDialog);
                if (dialogResult == DialogResult.OK)
                {
                    var licenseText = NonLockingFileReader.ReadAllTextWithoutLocking(openDialog.FileName);
                    try
                    {
                        LicenseVerifier.Verify(licenseText);
                        var license = LicenseDeserializer.Deserialize(licenseText);

                        if (LicenseExpirationChecker.HasLicenseExpired(license))
                        {
                            var message = "The license you provided has expired, please select another file.";
                            Logger.Warn(message);
                            MessageBox.Show(this, message, "License expired", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                            return;
                        }
                        ResultingLicenseText = licenseText;
                        Close();
                    }
                    catch (Exception exception)
                    {
                        var message = string.Format("An error occurred when parsing the license.\r\nMessage: {0}\r\nThe exception details have been appended to your log.", exception.Message);
                        Logger.Warn("Error parsing license", exception);
                        MessageBox.Show(this, message, "Error parsing license", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        public string ResultingLicenseText;

        void PurchaseButton_Click(object sender, EventArgs e)
        {
            Process.Start("http://particular.net/licensing");
        }

        private void getTrialLicenseButton_Click(object sender, EventArgs e)
        {
            if (CurrentLicense != null && CurrentLicense.IsTrialLicense)
            {
                Process.Start("http://particular.net/extend-your-trial-14");
            }
            else
            {
                Process.Start("http://particular.net/extend-your-trial-45");
            }
        }
    }
}