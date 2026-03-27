namespace NServiceBus.TransportTests;

#pragma warning disable CS1591
public partial class TransportTestsConfiguration
{
    public IConfigureTransportInfrastructure CreateTransportConfiguration() => new ConfigureInMemoryTransportInfrastructure();
}
#pragma warning restore CS1591
