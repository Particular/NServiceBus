namespace NServiceBus
{
    using AcceptanceTesting;

    public static class AcceptanceTestingTransportExtensions
    {
        public static TransportExtensions<AcceptanceTestingTransport> UseNativeDelayedDelivery(this TransportExtensions<AcceptanceTestingTransport> transportExtensions, bool useNativeDelayedDelivery)
        {
            Guard.AgainstNull(nameof(transportExtensions), transportExtensions);

            transportExtensions.Settings.Set(AcceptanceTestingTransportInfrastructure.UseNativeDelayedDeliveryKey, useNativeDelayedDelivery);
            return transportExtensions;
        }

        public static TransportExtensions<AcceptanceTestingTransport> UseNativePubSub(this TransportExtensions<AcceptanceTestingTransport> transportExtensions, bool useNativePubSub)
        {
            Guard.AgainstNull(nameof(transportExtensions), transportExtensions);

            transportExtensions.Settings.Set(AcceptanceTestingTransportInfrastructure.UseNativePubSubKey, useNativePubSub);
            return transportExtensions;
        }

        public static TransportExtensions<AcceptanceTestingTransport> StorageDirectory(this TransportExtensions<AcceptanceTestingTransport> transportExtensions, string path)
        {
            Guard.AgainstNull(nameof(transportExtensions), transportExtensions);
            Guard.AgainstNullAndEmpty(nameof(path), path);
            PathChecker.ThrowForBadPath(path, "StorageDirectory");

            transportExtensions.Settings.Set(AcceptanceTestingTransportInfrastructure.StorageLocationKey, path);
            return transportExtensions;
        }
    }
}