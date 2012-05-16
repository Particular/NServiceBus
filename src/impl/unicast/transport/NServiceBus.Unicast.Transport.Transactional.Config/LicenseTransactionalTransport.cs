using NServiceBus.Licensing;

namespace NServiceBus.Unicast.Transport.Transactional.Config
{
    public static class LicenseTransactionalTransport
    {
        public const string AllowedCoresLicenseKey = "AllowedCores";
        public const int MaxAllowedThreads = 1024;
        public const int SingleWorkerThread = 1;
        
        internal static int GetLicensingAllowedCores()
        {
            var license = LicenseManager.CurrentLicense;
            if (license.LicenseType == LicenseType.Basic1)
                return SingleWorkerThread;

            int allowedCores = MaxAllowedThreads;
            if (license.LicenseAttributes == null)
                return allowedCores;

            if (license.LicenseAttributes.ContainsKey(AllowedCoresLicenseKey))
                int.TryParse(license.LicenseAttributes[AllowedCoresLicenseKey], out allowedCores);

            return allowedCores;
        }

    }
}
