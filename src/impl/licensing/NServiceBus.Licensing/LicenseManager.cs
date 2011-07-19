using Common.Logging;
using Rhino.Licensing;

namespace NServiceBus.Licensing
{
    public class LicenseManager
    {
        public LicenseManager()
        {
            Validator = CreateValidator();
        }

        protected LicenseValidator CreateValidator()
        {
            return new LicenseValidator(LicenseDescriptor.PublicKey, LicenseDescriptor.LocalLicenseFile);
        }

        public bool Validate()
        {
            Logger.Info("Checking available license...");

            try
            {
                Validator.AssertValidLicense();

                Logger.InfoFormat("Found a {0} license.", Validator.LicenseType);
                Logger.InfoFormat("Registered to {0}", Validator.Name);
                Logger.InfoFormat("Expires on {0}", Validator.ExpirationDate);
                Logger.InfoFormat("Allowed to use {0} cores.", Validator.LicenseAttributes["AllowedCores"]);

                return true;
            }
            catch (LicenseNotFoundException)
            {
                string error = "The installed license is not valid";

                if (Validator.LicenseType == LicenseType.Trial)
                    error = error + "The trial period has expired!";

                Logger.Warn(error);
            }
            catch (LicenseFileNotFoundException)
            {
                Logger.Warn("This application requires a valid license to run.");
            }

            return false;
        }

        public LicenseValidator Validator
        {
            get; private set;
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(LicenseManager).Namespace);
    }
}