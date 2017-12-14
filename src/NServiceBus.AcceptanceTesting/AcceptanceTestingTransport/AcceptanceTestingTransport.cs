namespace NServiceBus
{
    using Routing;
    using Settings;
    using Transport;

    public class AcceptanceTestingTransport : TransportDefinition, IMessageDrivenSubscriptionTransport
    {
        public override bool RequiresConnectionString => false;

        public override string ExampleConnectionStringForErrorMessage { get; } = "";

        public override TransportInfrastructure Initialize(SettingsHolder settings, string connectionString)
        {
            Guard.AgainstNull(nameof(settings), settings);

            var useNativePubSub = settings.GetOrDefault<bool>("AcceptanceTestingTransport.UseNativePubSub");
            var useNativeDelayedDelivery = settings.GetOrDefault<bool>("AcceptanceTestingTransport.UseNativeDelayedDelivery");

            return new AcceptanceTestingTransportInfrastructure(settings, useNativePubSub, useNativeDelayedDelivery);
        }
    }
}