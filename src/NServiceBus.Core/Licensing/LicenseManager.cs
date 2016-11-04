namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using Logging;
    using Microsoft.Win32;
    using Particular.Licensing;

    class LicenseManager
    {
        internal bool HasLicenseExpired()
        {
            return license == null || LicenseExpirationChecker.HasLicenseExpired(license);
        }

        internal void InitializeLicense(string licenseText)
        {
            //only do this if not been configured by the fluent API
            if (licenseText == null)
            {
                licenseText = GetExistingLicense();
            }

            if (string.IsNullOrWhiteSpace(licenseText))
            {
                if (Debugger.IsAttached && Environment.UserInteractive && NeedsReporting())
                {
                    // Adding Ignore() explicitly to have a fire and forget model for invoking the web api.
                    TrackFirstTimeUsageEvent().Ignore();
                }
                license = GetTrialLicense();
                PromptUserForLicenseIfTrialHasExpired();
                return;
            }

            LicenseVerifier.Verify(licenseText);

            var foundLicense = LicenseDeserializer.Deserialize(licenseText);

            if (LicenseExpirationChecker.HasLicenseExpired(foundLicense))
            {
                // If the found license is a trial license then it is actually a extended trial license not a locally generated trial.
                // Set the property to indicate that it is an extended license as it's not set by the license generation 
                if (foundLicense.IsTrialLicense)
                {
                    foundLicense.IsExtendedTrial = true;
                    PromptUserForLicenseIfTrialHasExpired();
                    return;
                }
                Logger.Fatal("Your license has expired! You can renew it at https://particular.net/licensing.");
                return;
            }

            if (foundLicense.UpgradeProtectionExpiration != null)
            {
                Logger.InfoFormat("License upgrade protection expires on: {0}", foundLicense.UpgradeProtectionExpiration);
            }
            else
            {
                Logger.InfoFormat("License expires on {0}", foundLicense.ExpirationDate);
            }

            license = foundLicense;
        }

        static bool NeedsReporting()
        {
            // Check for the presence of HKCU\Software\NServiceBus
            using (var nsbRootRegKey = Registry.CurrentUser.OpenSubKey(@"Software\NServiceBus"))
            {
                if (nsbRootRegKey != null) return false;
            }

            //Check for the presence of HKCU\Software\ParticularSoftware
            using (var particularRegKey = Registry.CurrentUser.OpenSubKey(@"Software\ParticularSoftware"))
            {
                // Check for the presence of HKCU\Software\ParticularSoftware\PlatformInstaller
                using (var platformInstallerRegKey = particularRegKey?.OpenSubKey(@"Software\ParticularSoftware"))
                {
                    if (platformInstallerRegKey != null) return false;
                }

                // Check if NuGetUser value is set. Previous versions of NServiceBus Nuget installs creates this value.
                var isNsbPreviouslyInstalled = particularRegKey?.GetValue("NuGetUser");
                if (isNsbPreviouslyInstalled != null) return false;
            }
            return true;
        }

        static async Task TrackFirstTimeUsageEvent()
        {
            // Set the regisry key for NuGetUser and then call the web api. Web api does not need to succeed.
            // We only attempt once. Future executions will check the presence of this value to 
            // ensure that we don't call the web api more than once.
            SetupFirstTimeRegistryKeys();

            // Get the current version of NServiceBus that's being used
            var version = GitFlowVersion.MajorMinorPatch;

            // Report first time usage metric
            Logger.InfoFormat("Reporting first time usage and version information to www.particular.net. This call does not collect any personal information. For more details, see the License Agreement and the Privacy Policy available here: http://particular.net/licenseagreement. This call will NOT be executed in production servers. It is invoked only once when run in an interactive debugging mode when the endpoint is executed for the very first time.");
            const string webApiUrl = "https://particular.net/api/ReportFirstTimeUsage";
            var postData = $"version={version}";
            try
            {
                // Call the web api. 
                using (var httpClient = new HttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, webApiUrl)
                    {
                        Content = new StringContent(postData, Encoding.UTF8, "application/x-www-form-urlencoded")
                    };

                    var response = await httpClient.SendAsync(request).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                }
            }
            catch (Exception ex)
            {
                // For the end-user, this is not really an error that affects them. 
                Logger.InfoFormat("Could not report first time usage statistics to www.particular.net: {0}", ex);
            }
        }

        static void SetupFirstTimeRegistryKeys()
        {
            using (var regRoot = Registry.CurrentUser.CreateSubKey(@"Software\ParticularSoftware"))
            {
                regRoot?.SetValue("NuGetUser", "true");
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
                var message = $"Trial for the Particular Service Platform has been active since {trialStartDate.ToLocalTime().ToShortDateString()}.";
                Logger.Info(message);
            }

            return trialLicense;
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

        void PromptUserForLicenseIfTrialHasExpired()
        {
            if (!(Debugger.IsAttached && SystemInformation.UserInteractive))
            {
                //We only prompt user if user is in debugging mode and we are running in interactive mode
                return;
            }

            bool createdNew;
            using (new Mutex(true, $"NServiceBus-{GitFlowVersion.MajorMinor}", out createdNew))
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

        License license;

        static ILog Logger = LogManager.GetLogger(typeof(LicenseManager));
    }
}