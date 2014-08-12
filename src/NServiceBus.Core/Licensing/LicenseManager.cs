namespace NServiceBus.Licensing
{
    using System.Diagnostics;
    using System.Threading;
    using System.Windows.Forms;
    using Logging;
    using Microsoft.Win32;
    using Particular.Licensing;

    static class LicenseManager
    {
        internal static bool HasLicenseExpired()
        {
            return license == null || LicenseExpirationChecker.HasLicenseExpired(license);
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

                if (license == null || LicenseExpirationChecker.HasLicenseExpired(license))
                {
                    var licenseProvidedByUser = LicenseExpiredFormDisplayer.PromptUserForLicense(license);

                    if (licenseProvidedByUser != null)
                    {
                        license = licenseProvidedByUser;
                    }
                }
            }
        }

        static License GetTrialLicense()
        {
            var trialStartDate = TrialStartDateStore.GetTrialStartDate();
            var trialLicense = License.TrialLicense(trialStartDate);

            //Check trial is still valid
            if (LicenseExpirationChecker.HasLicenseExpired(trialLicense))
            {
                Logger.WarnFormat("Trial for the Particular Service Platform has expired");
            }
            else
            {
                var message = string.Format("Trial for Particular Service Platform is still active, trial expires on {0}.", trialLicense.ExpirationDate.Value.ToLocalTime().ToShortDateString());
                Logger.Info(message);
            }

            return trialLicense;
        }

        internal static void InitializeLicense()
        {
            //only do this if not been configured by the fluent API
            if (licenseText == null)
            {
                licenseText = GetExistingLicense();
            }

            if (string.IsNullOrWhiteSpace(licenseText))
            {
                license = GetTrialLicense();
                return;
            }

            LicenseVerifier.Verify(licenseText);

            var foundLicense = LicenseDeserializer.Deserialize(licenseText);

            if (LicenseExpirationChecker.HasLicenseExpired(foundLicense))
            {
                Logger.Fatal(" You can renew it at http://particular.net/licensing.");
                return;
            }

            if (foundLicense.UpgradeProtectionExpiration != null)
            {
                Logger.InfoFormat("UpgradeProtectionExpiration: {0}", foundLicense.UpgradeProtectionExpiration);
            }
            else
            {
                Logger.InfoFormat("Expires on {0}", foundLicense.ExpirationDate);
            }

            license = foundLicense;
        }

        static string GetExistingLicense()
        {
            string existingLicense;

            //look in HKCU
            if (UserSidChecker.IsNotSystemSid() && new RegistryLicenseStore().TryReadLicense(out existingLicense))
            {
                return existingLicense;
            }

            //look in HKLM
            if (new RegistryLicenseStore(Registry.LocalMachine).TryReadLicense(out existingLicense))
            {
                return existingLicense;
            }

            return LicenseLocationConventions.TryFindLicenseText();
        }

        static ILog Logger = LogManager.GetLogger(typeof(LicenseManager));
        static string licenseText;
        static License license;


    }
}