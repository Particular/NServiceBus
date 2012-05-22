using System.Reflection;
using Common.Logging;
using Rhino.Licensing;

namespace NServiceBus.Licensing
{
    public class LicenseManager
    {
        public const string LicenseTypeKey = "LicenseType";
        public LicenseManager()
        {
            Validator = CreateValidator();
        }

        private LicenseValidator CreateValidator()
        {
            return new LicenseValidator(LicenseDescriptor.PublicKey, LicenseDescriptor.LocalLicenseFile);
        }

        internal bool Validate()
        {
            Logger.Info("Checking available license...");

            try
            {
                Validator.AssertValidLicense();

                Logger.InfoFormat("Found a {0} license.", Validator.LicenseType);
                Logger.InfoFormat("Registered to {0}", Validator.Name);
                Logger.InfoFormat("Expires on {0}", Validator.ExpirationDate);
                if ((Validator.LicenseAttributes != null) && (Validator.LicenseAttributes.Count > 0))
                    foreach (var licenseAttribute in Validator.LicenseAttributes)
                        Logger.InfoFormat("[{0}]: [{1}]", licenseAttribute.Key, licenseAttribute.Value);                            
                
                if (Validator.IsExpired)
                {
                    Logger.Warn("Your trial is expired. You are allowed to run on two CPU cores only.");
                }

                SetNServiceBusLicense();

                return true;
            }
            catch (LicenseNotFoundException)
            {
                string error = "The installed license is not valid";

                if (Validator.LicenseType == Rhino.Licensing.LicenseType.Trial)
                    error = error + "The trial period has expired!";

                Logger.Warn(error);
            }
            catch (LicenseFileNotFoundException)
            {
                Logger.Warn("No valid license file was found. The host will be limited to 1 worker thread.");
            }
            SetNServiceBusLicense();
            return false;
        }

        /// <summary>
        /// Set NSeriviceBus license information.
        /// </summary>
        private void SetNServiceBusLicense()
        {
            license = new License();
            switch (Validator.LicenseType)
            {
                case Rhino.Licensing.LicenseType.None:
                    license.LicenseType = LicenseType.Basic1;
                    break;
                case Rhino.Licensing.LicenseType.Standard:
                    GetLicenseType(LicenseType.Standard);
                    break;
                case Rhino.Licensing.LicenseType.Trial:
                    GetLicenseType(LicenseType.Trial);
                    break;
                default:
                    Logger.ErrorFormat("Got unexpected license type [{0}], setting Basic1 free license type.", 
                        Validator.LicenseType.ToString());
                    license.LicenseType = LicenseType.Basic1;
                    break;
            }

            if (Validator.LicenseAttributes != null)
                license.LicenseAttributes = Validator.LicenseAttributes;
        }

        private void GetLicenseType(string defaultLicenseType)
        {
            if ((Validator.LicenseAttributes == null) || (!Validator.LicenseAttributes.ContainsKey(LicenseTypeKey)) ||
                (string.IsNullOrEmpty(Validator.LicenseAttributes[LicenseTypeKey])))
                license.LicenseType = defaultLicenseType;
            else
                license.LicenseType = Validator.LicenseAttributes[LicenseTypeKey];
        }

        internal LicenseValidator Validator
        {
            get; private set;
        }

        private static License license ;
        /// <summary>
        /// Get current NServiceBus licensing information
        /// </summary>
        public static License CurrentLicense
        {
            get
            {
                if (license == null)
                {
                    var licenseManager = new LicenseManager();
                    licenseManager.Validate();
                    Configure.Instance.Configurer.RegisterSingleton<LicenseManager>(licenseManager);
                    ChangeRhinoLicensingLogLevelToWarn();
                }
                return license;
            }
        }
        private static void ChangeRhinoLicensingLogLevelToWarn()
        {
            Assembly rhinoLicensingAssembly = Assembly.GetAssembly(typeof(Rhino.Licensing.LicenseValidator));
            if (rhinoLicensingAssembly == null) return;
            log4net.Repository.ILoggerRepository rhinoLicensingRepository =
                log4net.LogManager.GetRepository(rhinoLicensingAssembly);

            if (rhinoLicensingRepository == null) return;

            var hier = (log4net.Repository.Hierarchy.Hierarchy)rhinoLicensingRepository;
            var licenseValidatorLogger = hier.GetLogger("Rhino.Licensing.LicenseValidator");
            if (licenseValidatorLogger == null) return;

            ((log4net.Repository.Hierarchy.Logger)licenseValidatorLogger).Level = hier.LevelMap["WARN"];

        }
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LicenseManager).Namespace);
    }
}