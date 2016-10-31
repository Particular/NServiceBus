namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Reflection;
    using System.Text;
    using System.Threading;
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
                // Display the trial start dialog.
                if (FirstTimeUser())
                {
                    TrackFirstTimeUsageEvent();
                    SetupFirstTimeRegistryKeys();
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

        static bool FirstTimeUser()
        {
            // Check for the presence of HKCU:Software/NServiceBus
            using (var regNsbRoot = Registry.CurrentUser.OpenSubKey(@"Software\NServiceBus"))
            {
                if (regNsbRoot == null)
                {
                    //Check for the presence of HKCU:SOFTWARE\ParticularSoftware
                    using (var regParticularRoot = Registry.CurrentUser.OpenSubKey(@"Software\ParticularSoftware"))
                    {
                        if (regParticularRoot == null)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }


        private void TrackFirstTimeUsageEvent()
        {
            // Get the current version of NServiceBus that's being used
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version.ToString();

            if (assembly.Location != null)
            {
                version = FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion;
            }

            try
            {
                var azureFuncUrl =
                    "https://first-time-nuget-user.azurewebsites.net/api/TrackNewUser?code=1bkjzkbyeuf1ed46l87yom9529yu5ykzt81mjx65kgtmmv4pldiyxzvfprxf9s3xqxykf279cnmi";

                var postData = $"{{\"version\":\"{version}\"}}";
                var client = new HttpClient();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var request = new HttpRequestMessage(HttpMethod.Post, azureFuncUrl);
                request.Content = new StringContent(postData, Encoding.UTF8, "application/json");
                var response = client.SendAsync(request).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();
            }
            catch (Exception)
            {
                Logger.WarnFormat("Could not report first time user stat to GoogleAnalytics.");
            }
        }

        void SetupFirstTimeRegistryKeys()
        {
            var regRoot = Registry.CurrentUser.CreateSubKey(@"Software\ParticularSoftware");
            regRoot?.SetValue("NuGetUser", "true");
            regRoot?.Close();
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