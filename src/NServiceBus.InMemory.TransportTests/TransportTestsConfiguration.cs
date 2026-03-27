namespace NServiceBus.TransportTests;

#pragma warning disable CS1591 // Test configuration
public partial class TransportTestsConfiguration : ITransportTestsConfiguration
{
    public IConfigureTransportInfrastructure CreateTransportConfiguration() => new ConfigureInMemoryTransportInfrastructure();
}
#pragma warning restore CS1591
