namespace NServiceBus.AcceptanceTests.FakeTransport
{
    using Settings;
    using Transport;

    public class FakeTransport : TransportDefinition
    {
        public override bool RequiresConnectionString => false;

        public override string ExampleConnectionStringForErrorMessage => null;

        public override TransportInfrastructure Initialize(SettingsHolder settings, string connectionString)
        {
            return new FakeTransportInfrastructure(settings);
        }
    }
}