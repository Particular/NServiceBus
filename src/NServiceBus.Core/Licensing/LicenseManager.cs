namespace NServiceBus.Licensing
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Windows.Forms;
    using Logging;

    public static class LicenseManager
    {


        public static void RecordIfLicenseHasExpiredInTheHeader(TransportMessage transportMessage)
        {
            string expirationReason;
            transportMessage.Headers[Headers.HasLicenseExpired] = LicenseExpirationChecker.HasLicenseExpired(License, out expirationReason).ToString().ToLower();
        }

        internal static void InitializeLicenseText(string license)
        {
            licenseText = license;
        }

        internal static void PromptUserForLicenseIfTrialHasExpired()
        {
            if (!(Debugger.IsAttached && SystemInformation.UserInteractive))
            {
                //We only prompt user if user is in debugging mode and we are running in interactive mode
                return;
            }

            bool createdNew;
            using (new Mutex(true, string.Format("NServiceBus-{0}", GitFlowVersion.MajorMinor), out createdNew))
            {
                if (!createdNew)
                {
                    //Dialog already displaying for this software version by another process, so we just use the already assigned license.
                    return;
                }

                if (LicenseExpirationChecker.HasLicenseDateExpired(License.ExpirationDate))
                {
                    license = LicenseExpiredFormDisplayer.PromptUserForLicense() ?? LicenseDeserializer.GetTrialLicense(License.ExpirationDate);
                }
            }
        }

        static void WriteLicenseInfo()
        {
            Logger.InfoFormat("Expires on {0}", License.ExpirationDate);
            if (License.UpgradeProtectionExpiration != null)
            {
                Logger.InfoFormat("UpgradeProtectionExpiration: {0}", License.UpgradeProtectionExpiration);
            }

            if (License.MaxThroughputPerSecond == LicenseDeserializer.MaxThroughputPerSecond)
            {
                Logger.Info("MaxThroughputPerSecond: unlimited");
            }
            else
            {
                Logger.InfoFormat("MaxThroughputPerSecond: {0}", License.MaxThroughputPerSecond);
            }

            if (License.AllowedNumberOfWorkerNodes == LicenseDeserializer.MaxWorkerNodes)
            {
                Logger.Info("AllowedNumberOfWorkerNodes: unlimited");
            }
            else
            {
                Logger.InfoFormat("AllowedNumberOfWorkerNodes: {0}", License.AllowedNumberOfWorkerNodes);
            }
        }

        static void ConfigureNServiceBusToRunInTrialMode()
        {
            if (UserSidChecker.IsNotSystemSid())
            {
                var trialExpirationDate = TrialLicenseReader.GetTrialExpirationFromRegistry();
                license = LicenseDeserializer.GetTrialLicense(trialExpirationDate);

                //Check trial is still valid
                if (LicenseExpirationChecker.HasLicenseDateExpired(trialExpirationDate))
                {
                    Logger.FatalFormat("Trial for NServiceBus v{0} has expired.", GitFlowVersion.MajorMinor);
                }
                else
                {
                    var message = string.Format("Trial for NServiceBus v{0} is still active, trial expires on {1}. Configuring NServiceBus to run in trial mode.", GitFlowVersion.MajorMinor, trialExpirationDate.ToLocalTime().ToShortDateString());
                    Logger.Info(message);
                }
                return;
            }
            Logger.Fatal("Could not access registry for the current user sid. Please ensure that the license has been properly installed.");
            license = LicenseDeserializer.GetTrialLicense(DateTime.Today);
        }

        internal static void InitializeLicense()
        {
            //only do this if not been configured by the fluent API
            if (licenseText == null)
            {
                licenseText = LicenseLocationConventions.TryFindLicenseText();
                if (string.IsNullOrWhiteSpace(licenseText))
                {
                    ConfigureNServiceBusToRunInTrialMode();
                    return;
                }
            }

            SignedXmlVerifier.VerifyXml(licenseText);
            license = LicenseDeserializer.Deserialize(licenseText);
            string message;
            if (LicenseExpirationChecker.HasLicenseExpired(license, out message))
            {
                message = message + " You can renew it at http://particular.net/licensing.";
                Logger.Fatal(message);
            }            
            WriteLicenseInfo();
        }

        static ILog Logger = LogManager.GetLogger(typeof(LicenseManager));

        public static License License
        {
            get
            {
                if (license == null)
                {
                    InitializeLicense();
                }
                return license;
            }
        }
        static string licenseText;
        static License license;
    }
}