namespace NServiceBus.AcceptanceTests.Routing
{
    using Configuration.AdvanceExtensibility;
    using NServiceBus.Routing;
    using Settings;
    using Transport;

    public static class MessageDrivenPubSubRoutingExtensions
    {
        public static RoutingSettings<MessageDrivenPubSubTransportDefinition> MessageDrivenPubSubRouting(this EndpointConfiguration endpointConfiguration)
        {
            return new RoutingSettings<MessageDrivenPubSubTransportDefinition>(endpointConfiguration.GetSettings());
        }

        public class MessageDrivenPubSubTransportDefinition : TransportDefinition, IMessageDrivenSubscriptionTransport
        {
            public override string ExampleConnectionStringForErrorMessage { get; }

            public override TransportInfrastructure Initialize(SettingsHolder settings, string connectionString)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}