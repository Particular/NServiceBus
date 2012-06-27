using NServiceBus.Licensing;

namespace NServiceBus.Unicast.Config
{
    /// <summary>
    /// Contains Unicast related licensing keys
    /// </summary>
    public static class LicenseConfig
    {
        /// <summary>
        /// Throttling message receiving according to licensing policy
        /// </summary>
        public const string MaxMessageThroughputPerSecondLicenseKey = "MaxMessageThroughputPerSecond";

        /// <summary>
        /// There is no licensing policy limit on message receiving throughput
        /// </summary>
        public const string MaxMessageThroughputPerSecond = "Max";
        /// <summary>
        /// Licensing policy limits on message receiving to maximum of 1 message per second
        /// </summary>
        public const int OneMessagePerSecondThroughput = 1;
        /// <summary>
        /// Licensing policy limits on message receiving to maximum of 2 message per second
        /// </summary>
        public const int TwoMessagePerSecondThroughput = 2;
        /// <summary>
        /// Licensing policy limits on message receiving to maximum of 4 message per second
        /// </summary>
        public const int FourMessagePerSecondThroughput = 4;
        /// <summary>
        /// Licensing policy limits on message receiving to maximum of 8 message per second
        /// </summary>
        public const int EightMessagePerSecondThroughput = 8;
        /// <summary>
        /// Licensing policy limits on message receiving to maximum of 16 message per second
        /// </summary>
        public const int SixteenMessagePerSecondThroughput = 16;
        /// <summary>
        /// Licensing policy limits on message receiving to maximum of 32 message per second
        /// </summary>
        public const int ThirtyTwoMessagePerSecondThroughput = 32;

        internal static int GetMaxThroughputPerSecond()
        {
            var license = LicenseManager.CurrentLicense;
            // Basic1 means there is no License file, so set throughput to one message per second.
            if (license.LicenseType == LicenseType.Basic1)
                return OneMessagePerSecondThroughput;

            if ((license.LicenseAttributes == null) || (license.LicenseAttributes.Count == 0))
                return 0;

            if (license.LicenseAttributes.ContainsKey(MaxMessageThroughputPerSecondLicenseKey))
            {
                string maxMessageThroughputPerSecond = license.LicenseAttributes[MaxMessageThroughputPerSecondLicenseKey];
                if (maxMessageThroughputPerSecond == MaxMessageThroughputPerSecond)
                    return 0;
                
                int messageThroughputPerSecond;
                if (int.TryParse(maxMessageThroughputPerSecond, out messageThroughputPerSecond))
                    return messageThroughputPerSecond;
            }
            return OneMessagePerSecondThroughput;
        }
    }
}