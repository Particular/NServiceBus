namespace NServiceBus.Licensing
{
    using System.Diagnostics;
    using System.Threading;
    using System.Windows.Forms;
    using Logging;

    public static class LicenseManager
    {
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
            using (new Mutex(true, string.Format("NServiceBus-{0}", NServiceBusVersion.MajorAndMinor), out createdNew))
            {
                if (!createdNew)
                {
                    //Dialog already displaying for this software version by another process, so we just use the already assigned license.
                    return;
                }

                //TODO: should we display dialog if UpgradeProtection is not valid?
                if (ExpiryChecker.IsExpired(License.ExpirationDate))
                {
                    License = LicenseExpiredFormDisplayer.PromptUserForLicense();
                }
            }
        }

        static void WriteLicenseInfo()
        {
            Logger.InfoFormat("Expires on {0}", License.ExpirationDate);
            if (License.UpgradeProtectionExpiration != null)
            {
                Logger.InfoFormat("UpgradeProtectionExpiration {0}", License.UpgradeProtectionExpiration);
            }
            Logger.InfoFormat("MaxThroughputPerSecond {0}", License.MaxThroughputPerSecond);
            Logger.InfoFormat("AllowedNumberOfWorkerNodes {0}", License.AllowedNumberOfWorkerNodes);
        }

        static void ConfigureNServiceBusToRunInTrialMode()
        {
            if (UserSidChecker.IsNotSystemSid())
            {
                var trialExpirationDate = TrialLicenseReader.GetTrialExpirationFromRegistry();
                //Check trial is still valid
                if (ExpiryChecker.IsExpired(trialExpirationDate))
                {
                    Logger.WarnFormat("Trial for NServiceBus v{0} has expired. Falling back to run in Basic1 license mode.", NServiceBusVersion.MajorAndMinor);

                    License = LicenseDeserializer.GetBasicLicense();
                }
                else
                {
                    var message = string.Format("Trial for NServiceBus v{0} is still active, trial expires on {1}. Configuring NServiceBus to run in trial mode.", NServiceBusVersion.MajorAndMinor, trialExpirationDate.ToLocalTime().ToShortDateString());
                    Logger.Info(message);

                    //Run in unlimited mode during trial period
                    License = LicenseDeserializer.GetTrialLicense(trialExpirationDate);
                }
                return;
            }

            Logger.Warn("Could not access registry for the current user sid. Falling back to run in Basic license mode.");

            License = LicenseDeserializer.GetBasicLicense();
        }

        internal static void Verify()
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
            var tempLicense = LicenseDeserializer.Deserialize(licenseText);

            string message;
            if (LicenseDowngrader.ShouldLicenseDowngrade(tempLicense, out message))
            {
                message = message + " You can renew it at http://particular.net/licensing. Downgrading to basic mode";
                Logger.Warn(message);
                License = LicenseDeserializer.GetBasicLicense();
            }
            else
            {
                License = tempLicense;
            }
            WriteLicenseInfo();
        }

        static ILog Logger = LogManager.GetLogger(typeof(LicenseManager));

        public static License License;
        static string licenseText;
    }
}