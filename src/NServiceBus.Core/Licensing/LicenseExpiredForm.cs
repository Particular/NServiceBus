namespace NServiceBus.Licensing
{
    using System;
    using System.Diagnostics;
    using System.Windows.Forms;
    using Janitor;
    using Logging;

    [SkipWeaving]
    partial class LicenseExpiredForm : Form
    {
        static ILog Logger = LogManager.GetLogger(typeof(LicenseExpiredForm));
        public LicenseExpiredForm()
        {
            InitializeComponent(); 
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Visible = true;
        }

        void browseButton_Click(object sender, EventArgs e)
        {
            using (var openDialog = new OpenFileDialog())
            {
                openDialog.InitializeLifetimeService();
                openDialog.Filter = "License files (*.xml)|*.xml|All files (*.*)|*.*";
                openDialog.Title = "Select License file";

                var dialogResult = StaThreadRunner.ShowDialogInSTA(() => { return openDialog.ShowDialog(); });
                if (dialogResult == DialogResult.OK)
                {
                    var licenseText = NonLockingFileReader.ReadAllTextWithoutLocking(openDialog.FileName);
                    try
                    {
                        SignedXmlVerifier.VerifyXml(licenseText);
                        var license = LicenseDeserializer.Deserialize(licenseText);

                        string downgradeReason;
                        if (LicenseDowngrader.ShouldLicenseDowngrade(license, out downgradeReason))
                        {
                            var message = string.Format("The license you provided has expired.\r\nReason:{0}\r\nClick 'Purchase' to obtain a new license. Or try a different file.\r\nThis massage has been appended to your log.", downgradeReason);
                            Logger.Warn(message);
                            MessageBox.Show(this, message, "License expired", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                            return;
                        }
                        MessageBox.Show(this, "The new license has bee been verified. It will now be stored in the Registry for future use.", "License applied", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ResultingLicenseText = licenseText;
                        Close();
                    }
                    catch (Exception exception)
                    {
                        var message = string.Format("An error occurred parsing the license.\r\nMessage: {0}\r\nThe exception details have appended to your log.", exception.Message);
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
    }
}