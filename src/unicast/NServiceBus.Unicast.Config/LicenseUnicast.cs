using NServiceBus.Licensing;

namespace NServiceBus.Unicast.Config
{
    /// <summary>
    /// Contains Unicast related licensing keys
    /// </summary>
    public static class LicenseUnicast
    {
        /// <summary>
        /// How many worker (message receiving) threads are allowed by licensing policy
        /// </summary>
        public const string AllowedCoresLicenseKey = "AllowedCores";
        /// <summary>
        /// Throttling message receiving according to licensing policy
        /// </summary>
        public const string MaxMessageThroughputPerSecondLicenseKey = "MaxMessageThroughputPerSecondLicenseKey";

        /// <summary>
        /// There is no licensing policy limit on message receiving throughput
        /// </summary>
        public const int MaxMessageThroughputPerSecond = 0;
        /// <summary>
        /// Licensing policy limits on message receiving to maximum of 1 message per second
        /// </summary>
        public const int OneMessagePerSecondThroughput = 800;
        /// <summary>
        /// Licensing policy limits on message receiving to maximum of 2 message per second
        /// </summary>
        public const int TwoMessagePerSecondThroughput = 400;
        /// <summary>
        /// Licensing policy limits on message receiving to maximum of 4 message per second
        /// </summary>
        public const int FourMessagePerSecondThroughput = 200;
        /// <summary>
        /// Licensing policy limits on message receiving to maximum of 8 message per second
        /// </summary>
        public const int EightMessagePerSecondThroughput = 100;

        internal static int GetMaxThroughputPerSecond()
        {
            var license = LicenseManager.CurrentLicense;
            // Basic1 means there is no License file, so set throughput to one message per second.
            if (license.LicenseType == LicenseType.Basic1)
                return OneMessagePerSecondThroughput;

            int maxThroughputPerSecond = MaxMessageThroughputPerSecond;
            if (license.LicenseAttributes == null)
                return maxThroughputPerSecond;

            if (license.LicenseAttributes.ContainsKey(MaxMessageThroughputPerSecondLicenseKey))
                int.TryParse(license.LicenseAttributes[MaxMessageThroughputPerSecondLicenseKey], out maxThroughputPerSecond);

            return maxThroughputPerSecond;
        }
    }
}