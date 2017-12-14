namespace NServiceBus
{
    public static class AcceptanceTestingTransportExtensions
    {
        public static TransportExtensions<AcceptanceTestingTransport> UseNativeDelayedDelivery(this TransportExtensions<AcceptanceTestingTransport> config, bool useNativeDelayedDelivery)
        {
            config.Settings.Set("AcceptanceTestingTransport.UseNativeDelayedDelivery", useNativeDelayedDelivery);
            return config;
        }

        public static TransportExtensions<AcceptanceTestingTransport> UseNativePubSub(this TransportExtensions<AcceptanceTestingTransport> config, bool useNativePubSub)
        {
            config.Settings.Set("AcceptanceTestingTransport.UseNativePubSub", useNativePubSub);
            return config;
        }

        public static TransportExtensions<AcceptanceTestingTransport> StorageDirectory(this TransportExtensions<AcceptanceTestingTransport> transportExtensions, string path)
        {
            Guard.AgainstNullAndEmpty(nameof(path), path);
            Guard.AgainstNull(nameof(transportExtensions), transportExtensions);
            PathChecker.ThrowForBadPath(path, "StorageDirectory");

            transportExtensions.Settings.Set(LearningTransportInfrastructure.StorageLocationKey, path);
            return transportExtensions;
        }
    }
}