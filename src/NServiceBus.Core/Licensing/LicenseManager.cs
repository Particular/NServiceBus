namespace NServiceBus.Licensing
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Security.Principal;
    using System.Threading;
    using System.Windows.Forms;
    using Forms;
    using Logging;
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

        private const int TRIAL_DAYS = 14;

        private static readonly ILog Logger = LogManager.GetLogger(typeof (LicenseManager));
        public static readonly Version SoftwareVersion = GetNServiceBusVersion();

        private License license;
        private bool trialPeriodHasExpired;
        private AbstractLicenseValidator validator;

        static LicenseManager instance;

        private LicenseManager()
        {
            validator = CreateValidator();

            Validate();
        }

        private LicenseManager(string licenseText)
        {
            validator = CreateValidator(licenseText);

            Validate();
        }

        /// <summary>
        /// Initializes the licensing system with the given license
        /// </summary>
        /// <param name="licenseText"></param>
        public static void Parse(string licenseText)
        {
            instance = new LicenseManager(licenseText);
        }

        /// <summary>
        ///     Get current NServiceBus licensing information
        /// </summary>
        public static License CurrentLicense
        {
            get
            {
                if(instance == null)
                    instance = new LicenseManager();
                
                return instance.license;
            }
        }

        /// <summary>
        /// Prompts the users if their trial license has expired
        /// </summary>
        public static void PromptUserForLicenseIfTrialHasExpired()
        {
            if (instance == null)
                instance = new LicenseManager();
              
            instance.PromptUserForLicenseIfTrialHasExpiredInternal();
        }

        private void PromptUserForLicenseIfTrialHasExpiredInternal()
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
                                    var selectedLicenseText = ReadAllTextWithoutLocking(s);
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
                                catch (UnauthorizedAccessException ex)
                                {
                                    Logger.Debug("Could not write to the registry.", ex);
                                    f.DisplayError();
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

        internal static string ReadAllTextWithoutLocking(string path)
        {
            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var textReader = new StreamReader(fileStream))
            {
                return textReader.ReadToEnd();
            }
        }

        private void Validate()
        {
            if (validator != null)
            {
                try
                {
                    validator.AssertValidLicense();

                    Logger.InfoFormat("Found a {0} license.", (object) validator.LicenseType);
                    Logger.InfoFormat("Registered to {0}", (object) validator.Name);
                    Logger.InfoFormat("Expires on {0}", (object) validator.ExpirationDate);
                    if ((validator.LicenseAttributes != null) && (validator.LicenseAttributes.Count > 0))
                        foreach (var licenseAttribute in validator.LicenseAttributes)
                            Logger.InfoFormat("[{0}]: [{1}]", licenseAttribute.Key, licenseAttribute.Value);

                    CheckIfNServiceBusVersionIsNewerThanLicenseVersion();
                    
                    ConfigureNServiceBusLicense();

                    return;
                }
                catch (LicenseExpiredException)
                {
                    trialPeriodHasExpired = true;
                    Logger.Error("License has expired.");
                }
                catch (LicenseNotFoundException)
                {
                    Logger.Error("License could not be loaded.");
                }
                catch (LicenseFileNotFoundException)
                {
                    Logger.Error("License could not be loaded.");
                }

                Logger.Warn("Falling back to run in Basic1 license mode.");
                RunInBasic1Mode(DateTime.UtcNow);

                return;
            }

            Logger.Info("No valid license found.");
            ConfigureNServiceBusToRunInTrialMode();
        }

        private void ConfigureNServiceBusToRunInTrialMode()
        {
            var trialExpirationDate = DateTime.UtcNow.Date;

            var windowsIdentity = WindowsIdentity.GetCurrent();
            if (windowsIdentity != null && windowsIdentity.User != null &&
                !windowsIdentity.User.IsWellKnown(WellKnownSidType.LocalSystemSid))
            {
                //If first time run, configure expire date
                try
                {
                    string trialStartDateString;
                    using (var registryKey = Registry.CurrentUser.CreateSubKey(String.Format(@"SOFTWARE\NServiceBus\{0}", SoftwareVersion.ToString(2))))
                    {
                        if (registryKey == null)
                        {
                            Logger.Warn("Falling back to run in Basic1 license mode.");

                            trialPeriodHasExpired = true;

                            //if trial expired, run in Basic1
                            RunInBasic1Mode(trialExpirationDate);
                        }

                        if ((trialStartDateString = (string) registryKey.GetValue("TrialStart", null)) == null)
                        {
                            trialStartDateString = DateTime.UtcNow.ToString("yyyy-MM-dd");
                            registryKey.SetValue("TrialStart", trialStartDateString, RegistryValueKind.String);

                            Logger.DebugFormat("First time running NServiceBus v{0}, setting trial license start.",
                                               SoftwareVersion.ToString(2));
                        }
                    }

                    var trialStartDate = DateTime.ParseExact(trialStartDateString, "yyyy-MM-dd",
                                                             CultureInfo.InvariantCulture,
                                                             DateTimeStyles.AssumeUniversal);

                    trialExpirationDate = trialStartDate.Date.AddDays(TRIAL_DAYS);
                }
                catch (UnauthorizedAccessException ex)
                {
                    Logger.Debug("Could not write to the registry. Because we didn't find a license file we assume the trial has expired.", ex);
                }
            }

            //Check trial is still valid
            if (trialExpirationDate > DateTime.UtcNow.Date)
            {
                Logger.DebugFormat("Trial for NServiceBus v{0} is still active, trial expires on {1}.",
                                   SoftwareVersion.ToString(2), trialExpirationDate.ToLocalTime().ToShortDateString());
                Logger.Info("Configuring NServiceBus to run in trial mode.");

                //Run in unlimited mode during trial period
                license = new License
                    {
                        LicenseType = LicenseType.Trial,
                        ExpirationDate = trialExpirationDate
                    };

                ConfigureLicenseBasedOnAttribute(license.LicenseType, new Dictionary<string, string>
                    {
                        {AllowedNumberOfWorkerNodesLicenseKey, UnlimitedNumberOfWorkerNodes},
                        {WorkerThreadsLicenseKey, MaxWorkerThreads},
                        {MaxMessageThroughputPerSecondLicenseKey, MaxMessageThroughputPerSecond},
                    });
            }
            else
            {
                Logger.DebugFormat("Trial for NServiceBus v{0} has expired.", SoftwareVersion.ToString(2));
                Logger.Warn("Falling back to run in Basic1 license mode.");

                trialPeriodHasExpired = true;

                //if trial expired, run in Basic1
                RunInBasic1Mode(trialExpirationDate);
            }
        }

        private void RunInBasic1Mode(DateTime trialExpirationDate)
        {
            license = new License
                {
                    LicenseType = LicenseType.Basic1, 
                    ExpirationDate = trialExpirationDate
                };

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

        private static AbstractLicenseValidator CreateValidator(string licenseText = "")
        {
            if (!String.IsNullOrEmpty(licenseText))
            {
                Logger.Info(@"Using license supplied via fluent API.");
                return new StringLicenseValidator(LicenseDescriptor.PublicKey, licenseText);
            }

            if (!String.IsNullOrEmpty(LicenseDescriptor.AppConfigLicenseString))
            {
                Logger.Info(@"Using embedded license supplied via config file AppSettings/NServiceBus/License.");
                licenseText = LicenseDescriptor.AppConfigLicenseString;
            }
            else if (!String.IsNullOrEmpty(LicenseDescriptor.AppConfigLicenseFile))
            {
                if (File.Exists(LicenseDescriptor.AppConfigLicenseFile))
                {
                    Logger.InfoFormat(@"Using license supplied via config file AppSettings/NServiceBus/LicensePath ({0}).", LicenseDescriptor.AppConfigLicenseFile);
                    licenseText = ReadAllTextWithoutLocking(LicenseDescriptor.AppConfigLicenseFile);
                }
            }
            else if (File.Exists(LicenseDescriptor.LocalLicenseFile))
            {
                Logger.InfoFormat(@"Using license in current folder ({0}).", LicenseDescriptor.LocalLicenseFile);
                licenseText = ReadAllTextWithoutLocking(LicenseDescriptor.LocalLicenseFile);
            }
            else if (File.Exists(LicenseDescriptor.OldLocalLicenseFile))
            {
                Logger.InfoFormat(@"Using license in current folder ({0}).", LicenseDescriptor.OldLocalLicenseFile);
                licenseText = ReadAllTextWithoutLocking(LicenseDescriptor.OldLocalLicenseFile);
            }
            else if (!String.IsNullOrEmpty(LicenseDescriptor.HKCULicense))
            {
                Logger.InfoFormat(@"Using embedded license found in registry [HKEY_CURRENT_USER\Software\NServiceBus\{0}\License].", SoftwareVersion.ToString(2));
                licenseText = LicenseDescriptor.HKCULicense;
            }
            else if (!String.IsNullOrEmpty(LicenseDescriptor.HKLMLicense))
            {
                Logger.InfoFormat(@"Using embedded license found in registry [HKEY_LOCAL_MACHINE\Software\NServiceBus\{0}\License].", SoftwareVersion.ToString(2));
                licenseText = LicenseDescriptor.HKLMLicense;
            }

            return String.IsNullOrEmpty(licenseText) ? null : new StringLicenseValidator(LicenseDescriptor.PublicKey, licenseText);
        }

        //if NServiceBus version > license version, throw an exception
        private void CheckIfNServiceBusVersionIsNewerThanLicenseVersion()
        {
            if (validator.LicenseType == Rhino.Licensing.LicenseType.None)
            {
                return;
            }

            if (validator.LicenseType == Rhino.Licensing.LicenseType.Trial)
            {
                return;
            }

            if (validator.LicenseAttributes.ContainsKey(LicenseVersionKey))
            {
                try
                {
                    var semver = GetNServiceBusVersion();
                    var licenseVersion = Version.Parse(validator.LicenseAttributes[LicenseVersionKey]);
                    if (licenseVersion >= semver)
                        return;
                }
                catch (Exception exception)
                {
                    throw new ConfigurationErrorsException(
                        "Your license is valid for an older version of NServiceBus. If you are still within the 1 year upgrade protection period of your original license, you should have already received a new license and if you haven’t, please contact customer.care@particular.net. If your upgrade protection has lapsed, you can renew it at http://particular.net/licensing",
                        exception);
                }
            }

            throw new ConfigurationErrorsException(
                "Your license is valid for an older version of NServiceBus. If you are still within the 1 year upgrade protection period of your original license, you should have already received a new license and if you haven’t, please contact customer.care@particular.net. If your upgrade protection has lapsed, you can renew it at http://particular.net/licensing");
        }

        private static Version GetNServiceBusVersion()
        {
            var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;

            return new Version(assemblyVersion.Major, assemblyVersion.Minor);
        }

        /// <summary>
        ///     Set NServiceBus license information.
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
                    Logger.ErrorFormat((string) "Got unexpected license type [{0}], setting Basic1 free license type.",
                                       (object) validator.LicenseType.ToString());
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
                var allowedNumberOfWorkerNodes = attributes[AllowedNumberOfWorkerNodesLicenseKey];
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
                var workerThreadsInLicenseFile = attributes[WorkerThreadsLicenseKey];

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
                var maxMessageThroughputPerSecond = attributes[MaxMessageThroughputPerSecondLicenseKey];
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