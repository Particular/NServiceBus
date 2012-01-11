using Common.Logging;
using Rhino.Licensing;

namespace NServiceBus.Licensing
{
    public class LicenseManager
    {
        private const int DefaultAllowedCores = 2;

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
            var allowedCpuCores = DefaultAllowedCores;

            try
            {
                Validator.AssertValidLicense();

                Logger.InfoFormat("Found a {0} license.", Validator.LicenseType);
                Logger.InfoFormat("Registered to {0}", Validator.Name);
                Logger.InfoFormat("Expires on {0}", Validator.ExpirationDate);

                if (Validator.IsExpired)
                {
                    Logger.Warn("Your trial is expired. You are allowed to run on two CPU cores only.");
                }
                else
                {
                    Logger.InfoFormat("You are allowed to run on {0} CPU cores.", Validator.AllowedCores);
                    allowedCpuCores = Validator.AllowedCores;
                }

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
                Logger.Warn("No valid license file was found. The host will be limited to 1 worker thread.");
            }

            UpdateCpuCores(allowedCpuCores);
            return false;
        }

        public LicenseValidator Validator
        {
            get; private set;
        }

        private void UpdateCpuCores(int allowedCpuCores)
        {
            //NOTE: Update allowed CPU cores.
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(LicenseManager).Namespace);
    }
}