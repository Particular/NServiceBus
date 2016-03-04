namespace NServiceBus
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
                instructionsText.Text = "To extend the free trial, click 'Extend trial' and register online. When the license file is received, save it to disk and then click the 'Browse' button below to select it.";
                getTrialLicenseButton.Text = "Extend Trial";
                purchaseButton.Visible = false;
                getTrialLicenseButton.Left = purchaseButton.Left;
            }
            else
            {
                Text = "NServiceBus - Extended Trial Expired";
                instructionsText.Text = "Click 'Contact Sales' to request an extension to the free trial, or click 'Buy Now' to purchase a license online. When the license file is received, save it to disk and then click the 'Browse' button below to select it.";
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
                            var message = "The license you provided has expired, select another file.";
                            Logger.Warn(message);
                            MessageBox.Show(this, message, "License expired", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                            return;
                        }
                        ResultingLicenseText = licenseText;
                        Close();
                    }
                    catch (Exception exception)
                    {
                        var message = $"An error occurred when parsing the license.\r\nMessage: {exception.Message}\r\nThe exception details have been appended to the log.";
                        Logger.Warn("Error parsing license", exception);
                        MessageBox.Show(this, message, "Error parsing license", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        void PurchaseButton_Click(object sender, EventArgs e)
        {
            Process.Start("http://particular.net/licensing");
        }

        void getTrialLicenseButton_Click(object sender, EventArgs e)
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

        public string ResultingLicenseText;
        static ILog Logger = LogManager.GetLogger<LicenseExpiredForm>();
    }
}