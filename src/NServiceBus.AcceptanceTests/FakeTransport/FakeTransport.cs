namespace NServiceBus.AcceptanceTests.FakeTransport
{
    using Settings;
    using Transports;

    public class FakeTransport : TransportDefinition
    {
        protected override TransportInfrastructure Initialize(SettingsHolder settings, string connectionString)
        {
            return new FakeTransportInfrastructure(settings);
        }

        public override bool RequiresConnectionString => false;

        public override string ExampleConnectionStringForErrorMessage => null;
    }
}