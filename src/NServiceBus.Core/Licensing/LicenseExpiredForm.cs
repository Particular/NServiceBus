namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Windows.Forms;
    using Janitor;
    using Logging;
    using Microsoft.Win32;
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

            var version = GitVersionInformation.MajorMinorPatch;

            if (CurrentLicense != null && !CurrentLicense.IsExtendedTrial)
            {
                Text = $"NServiceBus License (v{version})";
                warningText.Text = "It's time to extend your trial.";
                instructionsText.Text = @"Your 14-day trial is up, but you can instantly extend your trial for FREE. NServiceBus will remain fully functional although continued use is in violation of our EULA.

When you receive your new license file, save it to disk and click the 'Browse' button to select it.";
                getTrialLicenseButton.Text = "Extend Trial";
                purchaseButton.Visible = false;
                getTrialLicenseButton.Left = purchaseButton.Left;
            }
            else
            {
                Text = $"NServiceBus License (v{version})";
                warningText.Text = "It's time to buy a license.";
                instructionsText.Text = @"Your extended trial is up, but we don't want to stop you in your tracks. If you need to extend your trial further, please contact us and we'll work something out. NServiceBus will remain fully functional although continued use is in violation of our EULA.

When you receive your new license file, save it to disk and click the 'Browse' button to select it.";
                getTrialLicenseButton.Text = "Contact Us";
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
                    var licenseText = NonBlockingReader.ReadAllTextWithoutLocking(openDialog.FileName);
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
            Process.Start("https://particular.net/licensing");
        }

        void getTrialLicenseButton_Click(object sender, EventArgs e)
        {
            string baseUrl;
            if (CurrentLicense != null && !CurrentLicense.IsExtendedTrial)
            {
                // Original 14 day trial expired, give the user a chance to extend trial
                baseUrl = "https://particular.net/extend-nservicebus-trial";
            }
            else
            {
                // Extended trial license expired, ask the user to Contact Sales
                baseUrl = "https://particular.net/extend-your-trial-45";
            }

            var trialStart = TrialStartDateStore.GetTrialStartDate().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var url = $"{baseUrl}?NugetUser={IsNugetUser()}&PlatformInstaller={HasUserInstalledPlatform()}&TrialStartDate={trialStart}";

            // Open the url with the querystrings
            Process.Start(url);
        }

        static string IsNugetUser()
        {
            using (var regRoot = Registry.CurrentUser.OpenSubKey(@"Software\ParticularSoftware"))
            {
                if (regRoot != null)
                {
                    return (string) regRoot.GetValue("NuGetUser", "false");
                }
            }
            return bool.FalseString;
        }

        static string HasUserInstalledPlatform()
        {
            using (var regRoot = Registry.CurrentUser.OpenSubKey(@"Software\ParticularSoftware\PlatformInstaller"))
            {
                if (regRoot != null)
                {
                    return bool.TrueString;
                }
            }
            return bool.FalseString;
        }

        public string ResultingLicenseText;
        static ILog Logger = LogManager.GetLogger<LicenseExpiredForm>();
    }
}