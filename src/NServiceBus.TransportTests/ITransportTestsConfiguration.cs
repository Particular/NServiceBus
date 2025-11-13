namespace NServiceBus.TransportTests;

public interface ITransportTestsConfiguration
{
    IConfigureTransportInfrastructure CreateTransportConfiguration();
}

public partial class TransportTestsConfiguration : ITransportTestsConfiguration
{
    public static TransportTestsConfiguration Current { get; } = new TransportTestsConfiguration();
}