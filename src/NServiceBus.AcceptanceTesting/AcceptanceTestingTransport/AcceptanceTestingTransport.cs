﻿namespace NServiceBus
{
    using AcceptanceTesting;
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

            return new AcceptanceTestingTransportInfrastructure(settings);
        }
    }
}