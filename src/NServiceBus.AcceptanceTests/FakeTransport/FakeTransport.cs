namespace NServiceBus.AcceptanceTests.FakeTransport
{
    using Settings;
    using Transports;

    public class FakeTransport : TransportDefinition
    {
        public override bool RequiresConnectionString => false;

        public override string ExampleConnectionStringForErrorMessage => null;

        protected override TransportInfrastructure Initialize(SettingsHolder settings, string connectionString)
        {
            return new FakeTransportInfrastructure(settings);
        }
    }
}