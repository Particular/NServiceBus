namespace NServiceBus
{
    using AcceptanceTesting;

    public static class AcceptanceTestingTransportExtensions
    {
        public static TransportExtensions<AcceptanceTestingTransport> UseNativeDelayedDelivery(this TransportExtensions<AcceptanceTestingTransport> config, bool useNativeDelayedDelivery)
        {
            config.Settings.Set(AcceptanceTestingTransportInfrastructure.UseNativeDelayedDeliveryKey, useNativeDelayedDelivery);
            return config;
        }

        public static TransportExtensions<AcceptanceTestingTransport> UseNativePubSub(this TransportExtensions<AcceptanceTestingTransport> config, bool useNativePubSub)
        {
            config.Settings.Set(AcceptanceTestingTransportInfrastructure.UseNativePubSubKey, useNativePubSub);
            return config;
        }

        public static TransportExtensions<AcceptanceTestingTransport> StorageDirectory(this TransportExtensions<AcceptanceTestingTransport> transportExtensions, string path)
        {
            Guard.AgainstNullAndEmpty(nameof(path), path);
            Guard.AgainstNull(nameof(transportExtensions), transportExtensions);
            PathChecker.ThrowForBadPath(path, "StorageDirectory");

            transportExtensions.Settings.Set(AcceptanceTestingTransportInfrastructure.StorageLocationKey, path);
            return transportExtensions;
        }
    }
}