namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    
    using System.Text;
#if NET452
    using System.Threading;
#endif
#if NETCOREAPP2_0
    using System.Runtime.InteropServices;
#endif
    using Logging;
    using Particular.Licensing;

    class LicenseManager
    {
        internal bool HasLicenseExpired()
        {
            return license == null || LicenseExpirationChecker.HasLicenseExpired(license);
        }

        internal void InitializeLicense(string licenseText, string licenseFilePath)
        {
            var licenseSources = LicenseSources.GetLicenseSources(licenseText, licenseFilePath);

            var result = ActiveLicense.Find("NServiceBus", licenseSources);
            license = result.License;

            LogFindResults(result);

            if (result.HasExpired)
            {
                if (license.IsTrialLicense)
                {
                    Logger.WarnFormat("Trial for the Particular Service Platform has expired.");
                    PromptUserForLicenseIfTrialHasExpired();
                }
                else
                {
                    Logger.Fatal("Your license has expired! You can renew it at https://particular.net/licensing.");
                }
            }
        }

        static void LogFindResults(ActiveLicenseFindResult result)
        {
            var report = new StringBuilder();

            if (debugLoggingEnabled)
            {
                report.AppendLine("Looking for license in the following locations:");

                foreach (var item in result.Report)
                {
                    report.AppendLine(item);
                }

                Logger.Debug(report.ToString());
            }
            else
            {
                foreach (var item in result.SelectedLicenseReport)
                {
                    report.AppendLine(item);
                }

                Logger.Info(report.ToString());
            }
        }

        void PromptUserForLicenseIfTrialHasExpired()
        {
            if (!(Debugger.IsAttached && Environment.UserInteractive))
            {
                //We only prompt user if user is in debugging mode and we are running in interactive mode
                return;
            }

#if NET452
            var licenseProvidedByUser = RequestLicenseFromPopupDialog();
#endif
#if NETCOREAPP2_0
            var licenseProvidedByUser = RequestLicenseFromConsole();
#endif

            if (licenseProvidedByUser != null)
            {
                license = licenseProvidedByUser;
            }
        }

#if NET452
        License RequestLicenseFromPopupDialog()
        {
            bool createdNew;
            using (new Mutex(true, $"NServiceBus-{GitFlowVersion.MajorMinor}", out createdNew))
            {
                if (!createdNew)
                {
                    //Dialog already displaying for this software version by another process, so we just use the already assigned license.
                    return null;
                }

                if (license == null || LicenseExpirationChecker.HasLicenseExpired(license))
                {
                    return LicenseExpiredFormDisplayer.PromptUserForLicense(license);
                }

                return null;
            }
        }
#endif

        static License RequestLicenseFromConsole()
        {
            Console.Clear();
            Console.WriteLine(@"
#################################################
        Thank you for using NServiceBus
  ---------------------------------------------
          Your trial license expired!
  ---------------------------------------------
    Press:
    [1] to extend your trial license for FREE
    [2] to purchase a license
    [3] to import a license
    [4] to continue without a license.
#################################################
");
            while (true)
            {
                var input = Console.ReadKey();
                switch (input.KeyChar)
                {
                    case '1':
                        OpenBrowser("https://particular.net/extend-nservicebus-trial");
                        break;
                    case '2':
                        OpenBrowser("https://particular.net/licensing");
                        break;
                    case '3':
                        throw new NotImplementedException();
                    case '4':
                        Console.WriteLine();
                        Console.WriteLine("Continuing without a license. NServiceBus will remain fully functional although continued use is in violation of our EULA.");
                        Console.WriteLine();
                        return null;
                    default:
                        break;
                }
            }
        }

        // taken from: https://brockallen.com/2016/09/24/process-start-for-urls-on-net-core/
        static void OpenBrowser(string url)
        {
#if NETCOREAPP2_0
            try
            {
                Process.Start(url);
            }
            catch
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    Console.WriteLine($"Unable to open '{url}'. Please enter the url manually into your browser.");
                }
            }
#endif
        }

        License license;

        static ILog Logger = LogManager.GetLogger(typeof(LicenseManager));
        static readonly bool debugLoggingEnabled = Logger.IsDebugEnabled;
    }
}