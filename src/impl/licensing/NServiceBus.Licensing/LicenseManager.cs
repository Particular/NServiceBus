namespace NServiceBus.Licensing
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Threading;
    using System.Windows.Forms;
    using Common.Logging;
    using Forms;
    using Microsoft.Win32;
    using Rhino.Licensing;

    public class LicenseManager
    {
        private const string LicenseTypeKey = "LicenseType";
        private const string LicenseVersionKey = "LicenseVersion";
        private const string MaxMessageThroughputPerSecondLicenseKey = "MaxMessageThroughputPerSecond";
        private const string MaxMessageThroughputPerSecond = "Max";
        private const int OneMessagePerSecondThroughput = 1;
        private const string WorkerThreadsLicenseKey = "WorkerThreads";
        private const string MaxWorkerThreads = "Max";
        private const int SingleWorkerThread = 1;
        private const int MaxNumberOfWorkerThreads = 1024;
        private const string AllowedNumberOfWorkerNodesLicenseKey = "AllowedNumberOfWorkerNodes";
        private const string UnlimitedNumberOfWorkerNodes = "Max";
        private const int MinNumberOfWorkerNodes = 2;

        private static readonly ILog Logger = LogManager.GetLogger(typeof (LicenseManager));
        public static readonly Version SoftwareVersion = GetNServiceBusVersion();

        private License license;
        private bool trialPeriodHasExpired;
        private AbstractLicenseValidator validator;

        public LicenseManager()
        {
            validator = CreateValidator();

            Validate();
        }

        /// <summary>
        ///     Get current NServiceBus licensing information
        /// </summary>
        public License CurrentLicense
        {
            get { return license; }
        }

        public void PromptUserForLicenseIfTrialHasExpired()
        {
            if (!(Debugger.IsAttached && SystemInformation.UserInteractive))
            {
                //We only prompt user if user is in debugging mode and we are running in interactive mode
                return;
            }

            bool createdNew;
            using (new Mutex(true, string.Format("NServiceBus-{0}", SoftwareVersion.ToString(2)), out createdNew))
            {
                if (!createdNew)
                {
                    //Dialog already displaying for this software version by another process, so we just use the already assigned license.
                    return;
                }

                //prompt user for license file
                if (trialPeriodHasExpired)
                {
                    bool validLicense;

                    using (var form = new TrialExpired())
                    {
                        form.CurrentLicenseExpireDate = license.ExpirationDate;

                        form.ValidateLicenseFile = (f, s) =>
                            {
                                StringLicenseValidator licenseValidator = null;

                                try
                                {
                                    string selectedLicenseText = File.ReadAllText(s);
                                    licenseValidator = new StringLicenseValidator(LicenseDescriptor.PublicKey,
                                                                                  selectedLicenseText);
                                    licenseValidator.AssertValidLicense();

                                    using (var registryKey = Registry.CurrentUser.CreateSubKey(String.Format(@"SOFTWARE\NServiceBus\{0}", SoftwareVersion.ToString(2))))
                                    {
                                        if (registryKey == null)
                                        {
                                            return false;
                                        }

                                        registryKey.SetValue("License", selectedLicenseText, RegistryValueKind.String);
                                    }

                                    return true;
                                }
                                catch (LicenseExpiredException)
                                {
                                    if (licenseValidator != null)
                                    {
                                        f.DisplayExpiredLicenseError(licenseValidator.ExpirationDate);
                                    }
                                    else
                                    {
                                        f.DisplayError();
                                    }
                                }
                                catch (Exception)
                                {
                                    f.DisplayError();
                                }

                                return false;
                            };

                        validLicense = form.ShowDialog() == DialogResult.OK;
                    }

                    if (validLicense)
                    {
                        //if user specifies a valid license file then run with that license
                        validator = CreateValidator();
                        Validate();
                    }
                }
            }
        }

        private void Validate()
        {
            Logger.Info("Checking available license...");

            if (validator != null)
            {
                try
                {
                    validator.AssertValidLicense();

                    Logger.InfoFormat("Found a {0} license.", validator.LicenseType);
                    Logger.InfoFormat("Registered to {0}", validator.Name);
                    Logger.InfoFormat("Expires on {0}", validator.ExpirationDate);
                    if ((validator.LicenseAttributes != null) && (validator.LicenseAttributes.Count > 0))
                        foreach (var licenseAttribute in validator.LicenseAttributes)
                            Logger.InfoFormat("[{0}]: [{1}]", licenseAttribute.Key, licenseAttribute.Value);

                    CheckIfNServiceBusVersionIsNewerThanLicenseVersion();

                    ConfigureNServiceBusLicense();

                    return;
                }
                catch (LicenseNotFoundException)
                {
                    Logger.Warn("No valid license found.");
                }
                catch (LicenseFileNotFoundException)
                {
                    Logger.Warn("No valid license found.");
                }
            }

            ConfigureNServiceBusToRunInTrialMode();
        }

        private void ConfigureNServiceBusToRunInTrialMode()
        {
            Logger.Info("No valid license found.");
            Logger.Info("Configuring NServiceBus to run in trial mode.");


            string trialStartDateString;

            //If first time run, configure expire date
            using (var registryKey = Registry.CurrentUser.CreateSubKey(String.Format(@"SOFTWARE\NServiceBus\{0}", SoftwareVersion.ToString(2))))
            {
                if (registryKey == null)
                {
                    return;
                }

                if ((trialStartDateString = (string) registryKey.GetValue("TrialStart", null)) == null)
                {
                    trialStartDateString = DateTime.UtcNow.ToString("yyyy-MM-dd");
                    registryKey.SetValue("TrialStart", trialStartDateString, RegistryValueKind.String);

                    Logger.DebugFormat("First time running NServiceBus v{0}, setting trial start.", SoftwareVersion.ToString(2));
                }
            }

            var trialStartDate = DateTime.ParseExact(trialStartDateString, "yyyy-MM-dd",
                                                          CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

            var trialExpirationDate = trialStartDate.Date.AddDays(21);

            //Check trial is still valid
            if (trialExpirationDate >  DateTime.UtcNow.Date)
            {
                Logger.DebugFormat("Trial for NServiceBus v{0} is still active, trial expires on {0}.", SoftwareVersion.ToString(2), trialExpirationDate.ToLocalTime().ToShortDateString());

                //Run in unlimited mode during trail period
                license = new License {LicenseType = LicenseType.Trial};
                license.ExpirationDate = trialExpirationDate;
                ConfigureLicenseBasedOnAttribute(license.LicenseType, new Dictionary<string, string>
                    {
                        {AllowedNumberOfWorkerNodesLicenseKey, UnlimitedNumberOfWorkerNodes},
                        {WorkerThreadsLicenseKey, MaxWorkerThreads},
                        {MaxMessageThroughputPerSecondLicenseKey, MaxMessageThroughputPerSecond},
                    });
            }
            else
            {
                Logger.DebugFormat("Trial for NServiceBus v{0} has expired, showing user dialog.", SoftwareVersion.ToString(2));

                trialPeriodHasExpired = true;

                //if trial expired, run in Basic1
                license = new License {LicenseType = LicenseType.Basic1};
                license.ExpirationDate = trialExpirationDate;
                ConfigureLicenseBasedOnAttribute(license.LicenseType, new Dictionary<string, string>
                    {
                        {
                            AllowedNumberOfWorkerNodesLicenseKey,
                            MinNumberOfWorkerNodes.ToString(CultureInfo.InvariantCulture)
                        },
                        {WorkerThreadsLicenseKey, SingleWorkerThread.ToString(CultureInfo.InvariantCulture)},
                        {
                            MaxMessageThroughputPerSecondLicenseKey,
                            OneMessagePerSecondThroughput.ToString(CultureInfo.InvariantCulture)
                        },
                    });
            }
        }

        private static AbstractLicenseValidator CreateValidator()
        {
            string licenseText = String.Empty;

            if (!String.IsNullOrEmpty(LicenseDescriptor.AppConfigLicenseString))
            {
                licenseText = LicenseDescriptor.AppConfigLicenseString;
            }
            else if (!String.IsNullOrEmpty(LicenseDescriptor.AppConfigLicenseFile))
            {
                if (File.Exists(LicenseDescriptor.AppConfigLicenseFile))
                {
                    licenseText = File.ReadAllText(LicenseDescriptor.AppConfigLicenseFile);
                }
            }
            else if (!String.IsNullOrEmpty(LicenseDescriptor.LocalLicenseFile) && File.Exists(LicenseDescriptor.AppConfigLicenseFile))
            {
                licenseText = File.ReadAllText(LicenseDescriptor.LocalLicenseFile);
            }
            else if (!String.IsNullOrEmpty(LicenseDescriptor.RegistryLicense))
            {
                licenseText = LicenseDescriptor.RegistryLicense;
            }

            return String.IsNullOrEmpty(licenseText) ? null : new StringLicenseValidator(LicenseDescriptor.PublicKey, licenseText);
        }

        //if NServiceBus version > license version, throw an exception
        private void CheckIfNServiceBusVersionIsNewerThanLicenseVersion()
        {
            if (validator.LicenseType == Rhino.Licensing.LicenseType.None)
                return;

            if (validator.LicenseAttributes.ContainsKey(LicenseVersionKey))
            {
                try
                {
                    Version semver = GetNServiceBusVersion();
                    Version licenseVersion = Version.Parse(validator.LicenseAttributes[LicenseVersionKey]);
                    if (licenseVersion >= semver)
                        return;
                }
                catch (Exception exception)
                {
                    throw new ConfigurationErrorsException(
                        "Your license is valid for an older version of NServiceBus. If you are still within the 1 year upgrade protection period of your original license, you should have already received a new license and if you haven’t, please contact customer.care@nservicebus.com. If your upgrade protection has lapsed, you can renew it at http://www.nservicebus.com/PurchaseSupport.aspx.",
                        exception);
                }
            }

            throw new ConfigurationErrorsException(
                "Your license is valid for an older version of NServiceBus. If you are still within the 1 year upgrade protection period of your original license, you should have already received a new license and if you haven’t, please contact customer.care@nservicebus.com. If your upgrade protection has lapsed, you can renew it at http://www.nservicebus.com/PurchaseSupport.aspx.");
        }

        private static Version GetNServiceBusVersion()
        {
            Version assembyVersion = Assembly.GetExecutingAssembly().GetName().Version;

            return new Version(assembyVersion.Major, assembyVersion.Minor);
        }

        /// <summary>
        ///     Set NSeriviceBus license information.
        /// </summary>
        private void ConfigureNServiceBusLicense()
        {
            license = new License();

            switch (validator.LicenseType)
            {
                case Rhino.Licensing.LicenseType.None:
                    license.LicenseType = LicenseType.Basic1;
                    break;
                case Rhino.Licensing.LicenseType.Standard:
                    SetLicenseType(LicenseType.Standard);
                    break;
                case Rhino.Licensing.LicenseType.Trial:
                    SetLicenseType(LicenseType.Trial);
                    break;
                default:
                    Logger.ErrorFormat("Got unexpected license type [{0}], setting Basic1 free license type.",
                                       validator.LicenseType.ToString());
                    license.LicenseType = LicenseType.Basic1;
                    break;
            }

            license.ExpirationDate = validator.ExpirationDate;

            ConfigureLicenseBasedOnAttribute(license.LicenseType, validator.LicenseAttributes);
        }

        private void ConfigureLicenseBasedOnAttribute(string licenseType, IDictionary<string, string> attributes)
        {
            license.MaxThroughputPerSecond = GetMaxThroughputPerSecond(licenseType, attributes);
            license.AllowedNumberOfThreads = GetAllowedNumberOfThreads(licenseType, attributes);
            license.AllowedNumberOfWorkerNodes = GetAllowedNumberOfWorkerNodes(license.LicenseType, attributes);
        }

        private static int GetAllowedNumberOfWorkerNodes(string licenseType, IDictionary<string, string> attributes)
        {
            if (licenseType == LicenseType.Basic1)
            {
                return MinNumberOfWorkerNodes;
            }

            if (attributes.ContainsKey(AllowedNumberOfWorkerNodesLicenseKey))
            {
                string allowedNumberOfWorkerNodes = attributes[AllowedNumberOfWorkerNodesLicenseKey];
                if (allowedNumberOfWorkerNodes == UnlimitedNumberOfWorkerNodes)
                {
                    return int.MaxValue;
                }

                int allowedWorkerNodes;
                if (int.TryParse(allowedNumberOfWorkerNodes, out allowedWorkerNodes))
                {
                    return allowedWorkerNodes;
                }
            }

            return MinNumberOfWorkerNodes;
        }

        private static int GetAllowedNumberOfThreads(string licenseType, IDictionary<string, string> attributes)
        {
            if (licenseType == LicenseType.Basic1)
            {
                return SingleWorkerThread;
            }

            if (attributes.ContainsKey(WorkerThreadsLicenseKey))
            {
                string workerThreadsInLicenseFile = attributes[WorkerThreadsLicenseKey];

                if (string.IsNullOrWhiteSpace(workerThreadsInLicenseFile))
                {
                    return SingleWorkerThread;
                }

                if (workerThreadsInLicenseFile == MaxWorkerThreads)
                {
                    return MaxNumberOfWorkerThreads;
                }

                int workerThreads;
                if (int.TryParse(workerThreadsInLicenseFile, out workerThreads))
                {
                    return workerThreads;
                }
            }

            return SingleWorkerThread;
        }

        private static int GetMaxThroughputPerSecond(string licenseType, IDictionary<string, string> attributes)
        {
            // Basic1 means there is no License file, so set throughput to one message per second.
            if (licenseType == LicenseType.Basic1)
            {
                return OneMessagePerSecondThroughput;
            }

            if (attributes.ContainsKey(MaxMessageThroughputPerSecondLicenseKey))
            {
                string maxMessageThroughputPerSecond = attributes[MaxMessageThroughputPerSecondLicenseKey];
                if (maxMessageThroughputPerSecond == MaxMessageThroughputPerSecond)
                {
                    return 0;
                }

                int messageThroughputPerSecond;
                if (int.TryParse(maxMessageThroughputPerSecond, out messageThroughputPerSecond))
                {
                    return messageThroughputPerSecond;
                }
            }

            return OneMessagePerSecondThroughput;
        }

        private void SetLicenseType(string defaultLicenseType)
        {
            if ((validator.LicenseAttributes == null) ||
                (!validator.LicenseAttributes.ContainsKey(LicenseTypeKey)) ||
                (string.IsNullOrEmpty(validator.LicenseAttributes[LicenseTypeKey])))
            {
                license.LicenseType = defaultLicenseType;
            }
            else
            {
                license.LicenseType = validator.LicenseAttributes[LicenseTypeKey];
            }
        }
    }
}